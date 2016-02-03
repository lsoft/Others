using System;
using System.Collections.Generic;
using System.Threading;
using Others.Disposer;
using Others.Helper;

namespace Others.ItemProvider.Queue
{
    /// <summary>
    /// Item provider with syncronization by Monitor.
    /// Поставщик очереди, блокирующийся с помощью монитора.
    /// </summary>
    /// <typeparam name="T">Item type. Тип итема в очереди</typeparam>
    public class MonitorWaitProvider<T> : IItemWaitProvider<T>, IDisposable
          where T : class
    {
        /// <summary>
        /// Awake worker thread event.
        /// Событие пробуждения потока
        /// </summary>
        private readonly ManualResetEvent _awakeEvent = new ManualResetEvent(false);

        /// <summary>
        /// Shutdown (stop working) event.
        /// Эвент завершения работы; его автоматически сбрасывать не надо
        /// </summary>
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        /// <summary>
        /// Item queue.
        /// Очередь итемов
        /// </summary>
        private readonly Queue<T> _queue = new Queue<T>();

        /// <summary>
        /// Dispose guard.
        /// Хелпер, помогающий задиспозить объект с защитой многопоточности
        /// </summary>
        private readonly IThreadSafeDisposer _disposer;

        /// <summary>
        /// Wait handles array.
        /// Массив ожидателей, чтобы не создавать его каждый раз внутри DoGetItem
        /// </summary>
        private readonly WaitHandle[] _waiters;

        public MonitorWaitProvider(
            IThreadSafeDisposer disposer
            )
        {
            if (disposer == null)
            {
                throw new ArgumentNullException("disposer");
            }

            _disposer = disposer;

            _waiters = new WaitHandle[]
            {
                _stopEvent,
                _awakeEvent
            };
        }

        public OperationResultEnum AddItem(
            T t
            )
        {
            return
                AddItem(
                    t,
                    TimeSpan.FromMilliseconds(-1)
                    );
        }

        public OperationResultEnum AddItem(
            T t,
            TimeSpan timeout
            )
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }

            var myResult = OperationResultEnum.Dispose;

            _disposer.DoWorkSafely(
                () =>
                {
                    myResult = DoAddItem(
                        t,
                        timeout
                        );
                });

            return
                myResult;
        }

        public OperationResultEnum GetItem(
            out T resultItem
            )
        {
            return
                GetItem(
                    TimeSpan.FromMilliseconds(-1),
                    out resultItem
                    );
        }

        public OperationResultEnum GetItem(
            TimeSpan timeout,
            out T resultItem
            )
        {
            var myResult = OperationResultEnum.Dispose;
            var myResultItem = default(T);

            _disposer.DoWorkSafely(
                () =>
                {
                    myResult = DoGetItem(
                        timeout,
                        out myResultItem
                        );
                });

            resultItem = myResultItem;

            return
                myResult;
        }

        public void Dispose()
        {
            //need to stop working threads
            //надо завершить уже рабочие триды, которые находятся на любом этапе своего выполнения

            //fire the shutdown event
            //принуждаем потоки проснуться и сигнализируем им что надо остановиться
            _stopEvent.Set();

            //do safe dispose
            //и диспозимся когда все триды завершатся
            _disposer.DoDisposeSafely(DoDispose);
        }


        #region private code

        private OperationResultEnum DoAddItem(T t, TimeSpan timeout)
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }

            if (Monitor.TryEnter(_queue, timeout))
            {
                try
                {
                    _queue.Enqueue(t);

                    if (_queue.Count == 1)
                    {
                        _awakeEvent.Set();
                    }

                }
                finally
                {
                    Monitor.Exit(_queue);
                }

                return
                    OperationResultEnum.Success;
            }

            return
                OperationResultEnum.Timeout;
        }

        private OperationResultEnum DoGetItem(TimeSpan timeout, out T resultItem)
        {
        repeat:
            var pt = new PerformanceTimer();
            var r = WaitHandle.WaitAny(
                _waiters,
                timeout
                );
            var waitTaken = pt.TimeInterval;

            if (r == 0)
            {
                resultItem = default(T);

                return
                    OperationResultEnum.Dispose;
            }

            if (r == System.Threading.WaitHandle.WaitTimeout)
            {
                resultItem = default(T);

                return
                    OperationResultEnum.Timeout;
            }

            OperationResultEnum result;

            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    resultItem = _queue.Dequeue();
                    result = OperationResultEnum.Success;
                }
                else
                {
                    _awakeEvent.Reset();

                    if (timeout.Ticks <= 0)
                    {
                        //there is no timeout, repeat waiting
                        //просто повторяем цикл ожидания, так как время не было задано
                        goto repeat; //MUHAHAHA!!!
                    }

                    //wait timeout must be corrected (we're waited some time)
                    //время ожидания было задано
                    //корректируем его

                    var tsWaitTaken = TimeSpan.FromSeconds(waitTaken);
                    timeout -= tsWaitTaken;

                    //in case of timeout < 0 it means we are timed out
                    //в принципе timeout может стать отрицательным или равным нулю, но System.Threading.WaitHandle.WaitTimeout не вернуться (ранее)
                    //это все равно означает, что таймаут вышел

                    if (timeout.Ticks > 0L)
                    {
                        //no timed out
                        //время ожидания не кончилось, отправляемся досыпать положенное
                        goto repeat; //MUHAHAHA!!!
                    }

                    //timed out
                    //время ожидания кончилось
                    result = OperationResultEnum.Timeout;
                    resultItem = default(T);
                }
            }

            return
                result;
        }

        private void DoDispose()
        {
            //safe dispose:
            //active worker threads has stopped
            //new worker threads aren't created
            //everything is green for dispose
            //ИТОГО:
            //рабочие потоки завершены
            //новые потоки не создаются
            //можно утилизироваться

            //dispose all disposable resources
            //утилизируем всё
            _awakeEvent.Dispose();
        }

        #endregion

    }
}
