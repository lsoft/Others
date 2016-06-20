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

            //����������� �������� _started �������� ����� �������� �������, 
            //��� ��� � ���������� ����� ���� ������, ���������� �� _started � _timer

            //��� ��� _timer.Restart ��� �����, ��������� � ������� ��������, � _started = true ��� ����
            //������ ��������, �� ��� �������� �� ����� ���� ��������������� (CLR ��������� ����� �����������)
            //��� ��� ������������� � ������� ������

            _started = true;

            _thread.Start();

            //������ �������
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

            //����������� ��������� ������ � �����������, ���� �� �� ���������
            if (_disposed)
            {
                return;
            }

            ISchedulerTask schedulerTask;

            //���� ����������� ��� ���������, �� ���� ��������������� ����� ����������� �� �������� ��� �������������� �������
            //��� ����������, ��� ��� � Task �������� ������ �����������, � �� ����������������� ���
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
            //����������� �������� ������ � ������������, ���� �� �� ���������
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

                //������ ������� ������ ���������
                OnStopping();
                try
                {
                    _waitGroup.RaiseEvent(WaitGroupEventEnum.Stop);
                    _thread.Join();

                    _waitGroup.Dispose();
                }
                finally
                {
                    //������ ������� ���������� ���������
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
                    //��������� ������ (����� "close" - �� ��������, � ���������)
                    var closedTask = _taskContainer.GetClosest();

                    long microsecondsToAwake; //������ (� �������������) � ������� ��� ������ ���� ���������
                    if (closedTask == null)
                    {
                        //���� ������ ��� - ���� ������������� ����� �� �������� _stopEvent ��� _restartEvent
                        microsecondsToAwake = -1;

                        //������ ������� ��� �����
                        OnNoTask();
                    }
                    else
                    {
                        //���������� ������� ����� ��������� ������
                        microsecondsToAwake = closedTask.MicrosecondsToAwake;

                        if (microsecondsToAwake < 0)
                        {
                            //�������� ������������� - ��� �������� ����� ��������!
                            //������ - �� ���� ������
                            microsecondsToAwake = 0L;
                        }
                    }

                    var waitResult = _waitGroup.WaitAny(microsecondsToAwake);

                    switch (waitResult)
                    {
                        case WaitGroupEventEnum.Stop:
                            //��������� �����������
                            return;

                        case WaitGroupEventEnum.Restart:
                            //���� ���������� � ��������� ����� ���������� ������
                            //������ ����� ������ ��� ����� ��� �������� ��� ������
                            break;

                        case WaitGroupEventEnum.WaitTimeout:
                            //������� ���������
                            if (closedTask != null)
                            {
                                var needToRepeat = false;
                                try
                                {
                                    //������ ������� ������ ���������� �����
                                    OnTaskBeginExecution(closedTask.TaskGuid);

                                    //�������� �����, ��������, ��� ���� ���� "���������"
                                    closedTask.Execute(
                                        out needToRepeat
                                        );
                                }
                                catch (Exception excp)
                                {
                                    //��������� ������ ���������� ������
                                    //�� ������ ���, � �������� �����. �����
                                    //���������� ������ ��� ���������, ���� �� ������ ��������� � ���
                                    OnTaskRaisedException(excp);
                                }
                                finally
                                {
                                    //������ ������� ���������� ���������� �����
                                    OnTaskEndExecution(closedTask.TaskGuid);

                                    if (!needToRepeat)
                                    {
                                        //������� ������ �� ��������
                                        //� ������ ������ ����� �� ����� ������� catch
                                        //��� ��������� OnCriticalException � ������� ����������� 
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
                //����������� ������ ������ ��������
                //����� ������� ����������� ������� �������� ��� ��������� ������� ��������
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
                //� ����� ������ ����������� ���
                //������� ������ ������� � ���
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
                //� ����� ������ ����������� ���
                //������� ������ ������� � ���
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
                    //���� � ���������� ��������� ����, �� ������ ����� �������� ��������� � �������� TaskRaisedException
                    //��� ��� ��� ����� ���� ������ ����
                    //������� ������ ����� � ���

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
            /// ��� �����������
            /// </summary>
            NoSubscribers,

            /// <summary>
            /// ������ � �������� ��������� ������
            /// </summary>
            HandleException,

            /// <summary>
            /// ������� ���������� �������
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

                //����������� �� ���������
                //�������������� ����� �� ����
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

                //����������� ��� ���������
                //���� ��������������� ����� ����������� �� �������� ��� �������������� �������
                //��� ����������, ��� ��� � SchedulerTask �������� ������ �����������, � �� ����������������� ���
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
                        //���� ��������� ������
                        //����������� � ����������� "����������" ������� ��� ���� �������� ��������

                        _preTime += _task.MicrosecondsBetweenAwakes;
                    }
                }
            }
        }

        #endregion
    }
}