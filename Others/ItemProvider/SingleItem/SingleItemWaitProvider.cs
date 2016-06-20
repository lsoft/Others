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
        private readonly AutoResetEvent _itemStored = new AutoResetEvent(false);

        /// <summary>
        /// Extract item event.
        /// Ёвент что новый итем извлечен; он должен автоматически сбрасыватьс€ после просыпани€
        /// </summary>
        private readonly AutoResetEvent _itemRemoved = new AutoResetEvent(true);

        /// <summary>
        /// Shutdown (stop working) event.
        /// Ёвент завершени€ работы; его автоматически сбрасывать не надо
        /// </summary>
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);

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
        
        public SingleItemWaitProvider(
            IThreadSafeDisposer disposer
            )
        {
            if (disposer == null)
            {
                throw new ArgumentNullException("disposer");
            }

            _disposer = disposer;
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
                        null,
                        timeout
                        );
                });

            return
                myResult;
        }

        public OperationResultEnum AddItem(
            T t,
            TimeSpan timeout,
            WaitHandle externalBreakHandle
            )
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }
            if (externalBreakHandle == null)
            {
                throw new ArgumentNullException("externalBreakHandle");
            }

            var myResult = OperationResultEnum.Dispose;

            _disposer.DoWorkSafely(
                () =>
                {
                    myResult = DoAddItem(
                        t,
                        externalBreakHandle,
                        timeout
                        );
                });

            return
                myResult;
        }

        public OperationResultEnum AddItem(
            T t,
            WaitHandle externalBreakHandle
            )
        {
            return
                AddItem(
                    t,
                    TimeSpan.FromMilliseconds(-1),
                    externalBreakHandle
                );
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
                        null,
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
            return 
                GetItem(
                    TimeSpan.FromMilliseconds(-1),
                    externalBreakHandle,
                    out resultItem
                );
        }

        public OperationResultEnum GetItem(
            TimeSpan timeout,
            WaitHandle externalBreakHandle,
            out T resultItem
            )
        {
            if (externalBreakHandle == null)
            {
                throw new ArgumentNullException("externalBreakHandle");
            }

            var myResult = OperationResultEnum.Dispose;
            var myResultItem = default(T);

            _disposer.DoWorkSafely(
                () =>
                {
                    myResult = DoGetItem(
                        timeout,
                        externalBreakHandle,
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
            _stop.Set();

            //do safe dispose
            //и диспозимс€ когда все триды завершатс€
            _disposer.DoDisposeSafely(DoDispose);
        }

        #region private code

        private OperationResultEnum DoAddItem(
            T t,
            WaitHandle externalBreakHandle,
            TimeSpan timeout
            )
        {
            if (t == null)
            {
                throw new ArgumentNullException("t");
            }
            //externalBreakHandle allowed to be null

            OperationResultEnum result;

            //ожидаем, что произойдет
            var r = WaitHandle.WaitAny(
                GetRemovedWaitHandles(externalBreakHandle),
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

                case 2:
                    //сработало внешнее условие прерывани€
                    result = OperationResultEnum.ExternalCondition;
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

                _itemStored.Set();
            }

            return
                result;
        }

        private OperationResultEnum DoGetItem(
            TimeSpan timeout,
            WaitHandle externalBreakHandle,
            out T resultItem
            )
        {
            //externalBreakHandle allowed to be null

            //ожидаем, что произойдет
            var r = WaitHandle.WaitAny(
                GetStoredWaitHandles(externalBreakHandle),
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

                    _itemRemoved.Set();
                    result = OperationResultEnum.Success;
                    break;

                case 2:
                    //сработало внешнее условие прерывани€
                    result = OperationResultEnum.ExternalCondition;
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

        private WaitHandle[] GetRemovedWaitHandles(
            WaitHandle externalBreakHandle
            )
        {
            if (externalBreakHandle == null)
            {
                var result = new WaitHandle[]
                {
                    _stop,
                    _itemRemoved
                };

                return
                    result;
            }
            else
            {
                var result = new WaitHandle[]
                {
                    _stop,
                    _itemRemoved,
                    externalBreakHandle
                };

                return
                    result;
            }
        }

        private WaitHandle[] GetStoredWaitHandles(
            WaitHandle externalBreakHandle
            )
        {
            if (externalBreakHandle == null)
            {
                var result = new WaitHandle[]
                {
                    _stop,
                    _itemStored
                };

                return
                    result;
            }
            else
            {
                var result = new WaitHandle[]
                {
                    _stop,
                    _itemStored,
                    externalBreakHandle
                };

                return
                    result;
            }
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
            _stop.Dispose();
            _itemStored.Dispose();
            _itemRemoved.Dispose();
        }

        #endregion
    }
}