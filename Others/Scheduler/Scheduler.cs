using System;
using System.Collections.Generic;
using System.Linq;
using Others.Helper;
using Others.Scheduler.Event;
using Others.Scheduler.Logger;
using Others.Scheduler.SchedulerThread;
using Others.Scheduler.SchedulerThread.Factory;
using Others.Scheduler.Task;
using Others.Scheduler.WaitGroup;

namespace Others.Scheduler
{
    public class Scheduler : IScheduler, IDisposable
    {
        private readonly ISchedulerLogger _logger;
        private readonly IWaitGroup _waitGroup;
        private readonly IThread _thread;

        private readonly TaskContainer _taskContainer;
        private readonly PerformanceTimer _timer;

        private volatile bool _started = false;
        private volatile bool _disposed = false;

        public event SchedulerEventDelegate SchedulerEvent;

        public int TaskCount
        {
            get
            {
                return
                    _taskContainer.TaskCount;
            }
        }

        public Scheduler(
            IWaitGroupFactory waitGroupFactory,
            IThreadFactory threadFactory,
            ISchedulerLogger logger
            )
        {
            if (waitGroupFactory == null)
            {
                throw new ArgumentNullException("waitGroupFactory");
            }
            if (threadFactory == null)
            {
                throw new ArgumentNullException("threadFactory");
            }

            _logger = logger;

            _taskContainer = new TaskContainer();
            _timer = new PerformanceTimer();

            _waitGroup = waitGroupFactory.CreateWaitGroup(
                _timer
                );

            _thread = threadFactory.CreateThread(
                DoWork
                );
        }

        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if (_started)
            {
                return;
            }

            _timer.Restart();

            //выставление признака _started делается после рестарта таймера, 
            //так как в добавлении таска есть логика, завязанная на _started и _timer

            //так как _timer.Restart это метод, связанный с записью значений, и _started = true это тоже
            //запись значений, то эти операции не могут быть переупорядочены (CLR запрещает такую оптимизацию)
            //тут нет необходимости в барьере памяти

            _started = true;

            _thread.Start();

            //рейзим событие
            OnStarted();
        }

        public void AddTask(
            ITask task
            )
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            //допускается добавлять задачи в планировщик, пока он не стартовал
            if (_disposed)
            {
                return;
            }

            ISchedulerTask schedulerTask;

            //если планировщик уже стартовал, то надо скорректировать время пробуждения на величину уже проработанного времени
            //это необходимо, так как в Task попадает МОМЕНТ пробуждения, а не продолжительность сна
            if (_started)
            {
                schedulerTask = new SchedulerTask(
                    task,
                    _timer
                    );
            }
            else
            {
                schedulerTask = new SchedulerTask(
                    task
                    );
            }

            _taskContainer.AddTask(schedulerTask);

            _waitGroup.RaiseEvent(WaitGroupEventEnum.Restart);
        }

        public void CancelTask(
            Guid taskGuid
            )
        {
            //допускается отменять задачи в планировщике, пока он не стартовал
            if (_disposed)
            {
                return;
            }
            
            _taskContainer.RemoveTask(taskGuid);

            _waitGroup.RaiseEvent(WaitGroupEventEnum.Restart);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                //рейзим событие начала остановки
                OnStopping();
                try
                {
                    _waitGroup.RaiseEvent(WaitGroupEventEnum.Stop);
                    _thread.Join();

                    _waitGroup.Dispose();
                }
                finally
                {
                    //рейзим событие завершения остановки
                    OnStopped();
                }
            }
        }

        private void DoWork()
        {
            try
            {
                while (true)
                {
                    //ближайшая задача (здесь "close" - не закрытая, а ближайшая)
                    var closedTask = _taskContainer.GetClosest();

                    long microsecondsToAwake; //момент (в микросекундах) в который эту задачу надо выполнить
                    if (closedTask == null)
                    {
                        //если задачи нет - ждем неопределенно долго до сработки _stopEvent или _restartEvent
                        microsecondsToAwake = -1;

                        //рейзим событие нет задач
                        OnNoTask();
                    }
                    else
                    {
                        //определяем сколько ждать ближайшей задачи
                        microsecondsToAwake = closedTask.MicrosecondsToAwake;

                        if (microsecondsToAwake < 0)
                        {
                            //ожидание отрицательное - уже просрали время сработки!
                            //значит - не ждем вообще
                            microsecondsToAwake = 0L;
                        }
                    }

                    var waitResult = _waitGroup.WaitAny(microsecondsToAwake);

                    switch (waitResult)
                    {
                        case WaitGroupEventEnum.Stop:
                            //приказано завершаться
                            return;

                        case WaitGroupEventEnum.Restart:
                            //надо проснуться и повторить поиск ближайшего эвента
                            //скорее всего потому что эвент был добавлен или удален
                            break;

                        case WaitGroupEventEnum.WaitTimeout:
                            //событие сработало
                            if (closedTask != null)
                            {
                                var needToRepeat = false;
                                try
                                {
                                    //рейзим событие начала выполнения таска
                                    OnTaskBeginExecution(closedTask.TaskGuid);

                                    //вызываем метод, указывая, что этот таск "выстрелил"
                                    closedTask.Execute(
                                        out needToRepeat
                                        );
                                }
                                catch (Exception excp)
                                {
                                    //произошла ошибка выполнения задачи
                                    //не логгим его, а вызываем соотв. эвент
                                    //обработчик эвента уже определит, надо ли писать ексцепшен в лог
                                    OnTaskRaisedException(excp);
                                }
                                finally
                                {
                                    //рейзим событие завершения выполнения таска
                                    OnTaskEndExecution(closedTask.TaskGuid);

                                    if (!needToRepeat)
                                    {
                                        //удаляем задачу из шедулера
                                        //в случае ошибки уйдем на самый внешний catch
                                        //где вызовется OnCriticalException и шедулер остановится 
                                        _taskContainer.RemoveTask(closedTask);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception excp)
            {
                //критическая ошибка работы шедулера
                //после которой выполняется останов шедулера без генерации эвентов останова
                OnCriticalException(excp);
            }
        }

        #region event related code

        private void OnStarted()
        {
            OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.Started
                    )
                );
        }

        private void OnStopping()
        {
            OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.Stopping
                    )
                );
        }

        private void OnStopped()
        {
            OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.Stopped
                    )
                );
        }

        private void OnTaskRaisedException(
            Exception excp
            )
        {
            if (excp == null)
            {
                throw new ArgumentNullException("excp");
            }

            var result = OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.TaskRaisedException,
                    excp
                    )
                );

            if (result == EventHandlerResultEnum.NoSubscribers)
            {
                //у этого эвента подписчиков нет
                //поэтому просто запишем в лог
                _logger.LogException(excp);
            }
        }

        private void OnNoTask()
        {
            OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.NoTask
                    )
                );
        }

        private void OnTaskBeginExecution(
            Guid taskGuid
            )
        {
            OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.TaskBeginExecution,
                    taskGuid
                    )
                );
        }

        private void OnTaskEndExecution(
            Guid taskGuid
            )
        {
            OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.TaskEndExecution,
                    taskGuid
                    )
                );
        }

        private void OnCriticalException(
            Exception excp
            )
        {
            if (excp == null)
            {
                throw new ArgumentNullException("excp");
            }

            var result = OnScheduler(
                new SchedulerEventDescription(
                    SchedulerEventTypeEnum.CriticalException,
                    excp
                    )
                );

            if (result == EventHandlerResultEnum.NoSubscribers)
            {
                //у этого эвента подписчиков нет
                //поэтому просто запишем в лог
                _logger.LogException(excp);
            }
        }

        private EventHandlerResultEnum OnScheduler(
            SchedulerEventDescription argument
            )
        {
            if (argument == null)
            {
                throw new ArgumentNullException("argument");
            }

            var result = EventHandlerResultEnum.Success;

            SchedulerEventDelegate handler = SchedulerEvent;
            if (handler != null)
            {
                try
                {
                    handler(argument);
                }
                catch (Exception excp)
                {
                    //если в инвокаторе произошел сбой, то нельзя снова вызывать инвокатор с событием TaskRaisedException
                    //так как это может быть вечный цикл
                    //поэтому просто пишем в лог

                    _logger.LogException(excp);

                    result = EventHandlerResultEnum.HandleException;
                }
            }
            else
            {
                result = EventHandlerResultEnum.NoSubscribers;
            }

            return
                result;
        }

        private enum EventHandlerResultEnum
        {
            /// <summary>
            /// Нет подписчиков
            /// </summary>
            NoSubscribers,

            /// <summary>
            /// Ошибка в процессе обработки эвента
            /// </summary>
            HandleException,

            /// <summary>
            /// Успешно обработано событие
            /// </summary>
            Success
        }

        #endregion

        #region support classes

        private class TaskContainer
        {
            private readonly SortedSet<ISchedulerTask> _set = new SortedSet<ISchedulerTask>(new TaskComparer());
            private readonly Dictionary<Guid, ISchedulerTask> _dict = new Dictionary<Guid, ISchedulerTask>();

            private readonly object _locker = new object();

            public int TaskCount
            {
                get
                {
                    lock (_locker)
                    {
                        return
                            _set.Count;
                    }
                }
            }

            public void AddTask(
                ISchedulerTask task
                )
            {
                if (task == null)
                {
                    throw new ArgumentNullException("task");
                }

                lock (_locker)
                {
                    _set.Add(task);
                    _dict.Add(task.TaskGuid, task);
                }
            }

            public ISchedulerTask GetClosest()
            {
                ISchedulerTask result;

                lock (_locker)
                {
                    result = _set.FirstOrDefault();
                }

                return
                    result;
            }

            public void RemoveTask(
                ISchedulerTask task
                )
            {
                if (task == null)
                {
                    throw new ArgumentNullException("task");
                }

                lock (_locker)
                {
                    _set.Remove(task);
                    _dict.Remove(task.TaskGuid);
                }
            }

            public void RemoveTask(
                Guid taskGuid
                )
            {
                lock (_locker)
                {
                    ISchedulerTask task;
                    if (_dict.TryGetValue(taskGuid, out task))
                    {
                        _set.Remove(task);
                        _dict.Remove(taskGuid);
                    }
                }
            }

            public class TaskComparer : IComparer<ISchedulerTask>
            {
                public int Compare(ISchedulerTask x, ISchedulerTask y)
                {
                    if (x == null)
                    {
                        throw new ArgumentNullException("x");
                    }
                    if (y == null)
                    {
                        throw new ArgumentNullException("y");
                    }

                    return
                        x.MicrosecondsToAwake.CompareTo(y.MicrosecondsToAwake);
                }

            }

        }

        public interface ISchedulerTask
        {
            Guid TaskGuid
            {
                get;
            }

            long MicrosecondsToAwake
            {
                get;
            }

            void Execute(
                out bool needToRepeat
                );
        }

        private class SchedulerTask : ISchedulerTask
        {
            private readonly ITask _task;
            
            private long _preTime;

            public Guid TaskGuid
            {
                get
                {
                    return
                        _task.TaskGuid;
                }
            }

            public long MicrosecondsToAwake
            {
                get
                {
                    var r = _task.MicrosecondsBetweenAwakes;

                    r += _preTime;

                    return
                        r;
                }
            }

            public SchedulerTask(
                ITask task

                )
            {
                if (task == null)
                {
                    throw new ArgumentNullException("task");
                }

                _task = task;

                //планировщик не стартовал
                //корректировать время не надо
                _preTime = 0L;
            }

            public SchedulerTask(
                ITask task,
                PerformanceTimer timer
                )
            {
                if (task == null)
                {
                    throw new ArgumentNullException("task");
                }
                if (timer == null)
                {
                    throw new ArgumentNullException("timer");
                }

                _task = task;

                //планировщик уже стартовал
                //надо скорректировать время пробуждения на величину уже проработанного времени
                //это необходимо, так как в SchedulerTask попадает МОМЕНТ пробуждения, а не продолжительность сна
                _preTime += timer.MicroSeconds;
            }

            public void Execute(
                out bool needToRepeat
                )
            {
                needToRepeat = false;
                try
                {
                    _task.Execute(
                        out needToRepeat
                        );
                }
                finally
                {
                    if (needToRepeat)
                    {
                        //надо повторить задачу
                        //досуммируем в аккумулятор "прошедшего" времени еще один интервал ожидания

                        _preTime += _task.MicrosecondsBetweenAwakes;
                    }
                }
            }
        }

        #endregion
    }
}