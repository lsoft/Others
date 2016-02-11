using System;
using System.Threading;
using Others.Disposer;

namespace Others.ItemProvider.SingleItem
{
    /// <summary>
    /// Single item container/provider. In case of no item - request will wait for a item, timeout or dispose.
    /// In case of an item is already contained new adding request will wait.
    /// ѕоставщик одного итема. ≈сли итема нет - при запросе он будет ожидать поступлени€.
    /// ≈сли итем есть, то при поступлении нового итема он будет ожидать извлечени€ уже добавленного
    /// итема.
    /// </summary>
    /// <typeparam name="T">“ип, который хранитс€ в этом контейнере</typeparam>
    public class SingleItemWaitProvider<T>
        : ISingleItemWaitProvider<T>
        where T  : class
    {

        /// <summary>
        /// Store item event.
        /// Ёвент что новый итем сохранен; он должен автоматически сбрасыватьс€ после просыпани€
        /// </summary>
        private readonly AutoResetEvent _itemStoredEvent = new AutoResetEvent(false);

        /// <summary>
        /// Extract item event.
        /// Ёвент что новый итем извлечен; он должен автоматически сбрасыватьс€ после просыпани€
        /// </summary>
        private readonly AutoResetEvent _itemRemovedEvent = new AutoResetEvent(true);

        /// <summary>
        /// Shutdown (stop working) event.
        /// Ёвент завершени€ работы; его автоматически сбрасывать не надо
        /// </summary>
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

        /// <summary>
        /// Dispose guard.
        /// ’елпер, помогающий задиспозить объект с защитой многопоточности
        /// </summary>
        private readonly IThreadSafeDisposer _disposer;

        /// <summary>
        /// Item to be stored.
        /// «начение, которое хранит контейнер
        /// </summary>
        private T _value;
        
        /// <summary>
        /// Wait handles array used in removing items.
        /// ћассив ожидателей, чтобы не создавать его каждый раз внутри DoAddItem
        /// </summary>
        private readonly WaitHandle[] _stopRemovedWaiters;

        /// <summary>
        /// Wait handles array used in adding items.
        /// ћассив ожидателей, чтобы не создавать его каждый раз внутри DoGetItem
        /// </summary>
        private readonly WaitHandle[] _stopStoredWaiters;

        public SingleItemWaitProvider(
            IThreadSafeDisposer disposer
            )
        {
            if (disposer == null)
            {
                throw new ArgumentNullException("disposer");
            }

            _disposer = disposer;

            _stopRemovedWaiters = new WaitHandle[]
            {
                _stopEvent,
                _itemRemovedEvent
            };

            _stopStoredWaiters = new WaitHandle[]
            {
                _stopEvent,
                _itemStoredEvent
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
            //надо завершить уже рабочие триды, которые наход€тс€ на любом этапе своего выполнени€

            //fire the shutdown event
            //вызываем сигнал остановки потоков
            _stopEvent.Set();

            //do safe dispose
            //и диспозимс€ когда все триды завершатс€
            _disposer.DoDisposeSafely(DoDispose);
        }

        #region private code

        private OperationResultEnum DoAddItem(
            T t,
            TimeSpan timeout
            )
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }

            OperationResultEnum result;

            //ожидаем, что произойдет
            var r = WaitHandle.WaitAny(
                _stopRemovedWaiters,
                timeout
                );

            switch (r)
            {
                case 0:
                    //сработал стоп-евент
                    //завершаем трид
                    result = OperationResultEnum.Dispose;
                    break;

                case 1:
                    //сработал эвент, что итем извлечен
                    result = OperationResultEnum.Success;
                    break;

                case System.Threading.WaitHandle.WaitTimeout:
                    //сработал таймаут ожидани€
                    result = OperationResultEnum.Timeout;
                    break;

                default:
                    //неопределенное событие
                    throw new InvalidOperationException(r.ToString());
            }


            if (result == OperationResultEnum.Success)
            {
                var oldValue = Interlocked.Exchange(ref _value, t);

                if (oldValue != null)
                {
                    throw new InvalidOperationException("—тарый итем не был забран, это кака€-то ошибка!");
                }

                _itemStoredEvent.Set();
            }

            return
                result;
        }

        private OperationResultEnum DoGetItem(
            TimeSpan timeout,
            out T resultItem
            )
        {
            //ожидаем, что произойдет
            var r = WaitHandle.WaitAny(
                _stopStoredWaiters,
                timeout
                );

            OperationResultEnum result;
            resultItem = default(T);

            switch (r)
            {
                case 0:
                    //сработал стоп-евент
                    result = OperationResultEnum.Dispose;
                    break;

                case 1:
                    //сработал эвент "итем поступил"
                    resultItem = Interlocked.Exchange(ref _value, null);

                    if (resultItem == null)
                    {
                        throw new InvalidOperationException("Ќовый итем равен null, это кака€-то ошибка");
                    }

                    _itemRemovedEvent.Set();
                    result = OperationResultEnum.Success;
                    break;

                case System.Threading.WaitHandle.WaitTimeout:
                    //сработал таймаут ожидани€
                    result = OperationResultEnum.Timeout;
                    break;

                default:
                    throw new InvalidOperationException(r.ToString());
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
            //»“ќ√ќ:
            //рабочие потоки завершены
            //новые потоки не создаютс€
            //можно утилизироватьс€

            //dispose all disposable resources
            //утилизируем всЄ
            _stopEvent.Dispose();
            _itemStoredEvent.Dispose();
            _itemRemovedEvent.Dispose();
        }

        #endregion
    }
}