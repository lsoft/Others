using System;
using System.Collections.Concurrent;
using System.Threading;
using Others.Disposer;

namespace Others.ItemProvider.Queue
{
    /// <summary>
    /// Item provider with syncronization by Semaphore.
    /// Поставщик очереди, блокирующийся с помощью семафора.
    /// </summary>
    /// <typeparam name="T">Тип итема в очереди</typeparam>
    public class SemaphoreWaitProvider<T> : IQueueWaitProvider<T>, IDisposable
          where T : class
    {
        /// <summary>
        /// Awake worker thread event.
        /// Событие пробуждения потока
        /// </summary>
        private readonly Semaphore _awakeSemaphore = new Semaphore(0, int.MaxValue);

        /// <summary>
        /// Shutdown (stop working) event.
        /// Эвент завершения работы; его автоматически сбрасывать не надо
        /// </summary>
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        /// <summary>
        /// Item queue.
        /// Очередь итемов
        /// </summary>
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

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

        public SemaphoreWaitProvider(
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
                _awakeSemaphore
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
            TimeSpan timeout,
            WaitHandle externalBreakHandle
            )
        {
            throw new NotImplementedException("Not implemented yet.");
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

        public OperationResultEnum GetItem(
            TimeSpan timeout,
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
            _stopEvent.Set();

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

            _queue.Enqueue(t);
            
            count = _queue.Count;

            _awakeSemaphore.Release();

            return
                OperationResultEnum.Success;
        }

        private OperationResultEnum DoGetItem(TimeSpan timeout, out T resultItem)
        {
            var r = WaitHandle.WaitAny(
                _waiters,
                timeout
                );

            switch (r)
            {
                case 0:
                    resultItem = default(T);
                    return
                        OperationResultEnum.Dispose;

                case 1:
                    if (!_queue.TryDequeue(out resultItem))
                    {
                        throw new InvalidOperationException("Не обнаружено итемов, это ошибка");
                    }

                    return
                        OperationResultEnum.Success;

                case System.Threading.WaitHandle.WaitTimeout:
                    resultItem = default(T);
                    return
                        OperationResultEnum.Timeout;

                default:
                    throw new InvalidOperationException(r.ToString());
            }
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
            _awakeSemaphore.Dispose();
            _stopEvent.Dispose();
        }

        #endregion

    }
}
