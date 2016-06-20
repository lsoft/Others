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
    public class MonitorWaitProvider<T> : IQueueWaitProvider<T>, IDisposable
          where T : class
    {
        /// <summary>
        /// Awake worker thread event.
        /// Событие пробуждения потока
        /// </summary>
        private readonly ManualResetEvent _awake = new ManualResetEvent(false);

        /// <summary>
        /// Shutdown (stop working) event.
        /// Эвент завершения работы; его автоматически сбрасывать не надо
        /// </summary>
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);

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
                _stop,
                _awake
            };
        }

        public OperationResultEnum AddItem(
            T t,
            out int estimatedCount
            )
        {
            return
                AddItem(
                    t,
                    TimeSpan.FromMilliseconds(-1),
                    out estimatedCount
                    );
        }

        public OperationResultEnum AddItem(
            T t,
            TimeSpan timeout,
            out int estimatedCount
            )
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }

            var myResult = OperationResultEnum.Dispose;

            int mycount = 0;
            _disposer.DoWorkSafely(
                () =>
                {
                    myResult = DoAddItem(
                        t,
                        timeout,
                        out mycount
                        );
                });

            estimatedCount = mycount;

            return
                myResult;
        }

        public OperationResultEnum AddItem(
            T t
            )
        {
            int count;

            return
                AddItem(
                    t,
                    TimeSpan.FromMilliseconds(-1),
                    out count
                    );
        }

        public OperationResultEnum AddItem(
            T t,
            TimeSpan timeout
            )
        {
            int count;

            return
                AddItem(
                    t,
                    timeout,
                    out count
                    );
        }

        public OperationResultEnum AddItem(
            T t,
            WaitHandle externalBreakHandle
            )
        {
            throw new NotImplementedException("Not implemented yet.");
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

        public OperationResultEnum GetItem(
            WaitHandle externalBreakHandle,
            out T resultItem
            )
        {
            throw new NotImplementedException("Not implemented yet.");
        }

        public void Dispose()
        {
            //need to stop working threads
            //надо завершить уже рабочие триды, которые находятся на любом этапе своего выполнения

            //fire the shutdown event
            //принуждаем потоки проснуться и сигнализируем им что надо остановиться
            _stop.Set();

            //do safe dispose
            //и диспозимся когда все триды завершатся
            _disposer.DoDisposeSafely(DoDispose);
        }


        #region private code

        private OperationResultEnum DoAddItem(
            T t,
            TimeSpan timeout,
            out int count
            )
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

                    count = _queue.Count;

                    if (count == 1)
                    {
                        _awake.Set();
                    }

                }
                finally
                {
                    Monitor.Exit(_queue);
                }

                return
                    OperationResultEnum.Success;
            }

            count = 0;

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
            var waitTaken = pt.Seconds;

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
                    _awake.Reset();

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
            _awake.Dispose();
            _stop.Dispose();
        }

        #endregion

    }
}
