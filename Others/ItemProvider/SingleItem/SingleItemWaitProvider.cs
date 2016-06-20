using System;
using System.Threading;
using Others.Disposer;

namespace Others.ItemProvider.SingleItem
{
    /// <summary>
    /// Single item container/provider. In case of no item - request will wait for a item, timeout or dispose.
    /// In case of an item is already contained new adding request will wait.
    /// Поставщик одного итема. Если итема нет - при запросе он будет ожидать поступления.
    /// Если итем есть, то при поступлении нового итема он будет ожидать извлечения уже добавленного
    /// итема.
    /// </summary>
    /// <typeparam name="T">Тип, который хранится в этом контейнере</typeparam>
    public class SingleItemWaitProvider<T>
        : ISingleItemWaitProvider<T>
        where T  : class
    {

        /// <summary>
        /// Store item event.
        /// Эвент что новый итем сохранен; он должен автоматически сбрасываться после просыпания
        /// </summary>
        private readonly AutoResetEvent _itemStored = new AutoResetEvent(false);

        /// <summary>
        /// Extract item event.
        /// Эвент что новый итем извлечен; он должен автоматически сбрасываться после просыпания
        /// </summary>
        private readonly AutoResetEvent _itemRemoved = new AutoResetEvent(true);

        /// <summary>
        /// Shutdown (stop working) event.
        /// Эвент завершения работы; его автоматически сбрасывать не надо
        /// </summary>
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);

        /// <summary>
        /// Dispose guard.
        /// Хелпер, помогающий задиспозить объект с защитой многопоточности
        /// </summary>
        private readonly IThreadSafeDisposer _disposer;

        /// <summary>
        /// Item to be stored.
        /// Значение, которое хранит контейнер
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
            WaitHandle externalBreakHandle
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
                        externalBreakHandle,
                        TimeSpan.FromMilliseconds(-1)
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
                        null,
                        out myResultItem
                        );
                });

            resultItem = myResultItem;

            return
                myResult;
        }

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// Получить итем. Если итемов нет - ждать указаный таймаут.
        /// </summary>
        /// <param name="externalBreakHandle">External break handle. Внешний хендл прерывания ожидания</param>
        /// <param name="resultItem">Extracted item if success otherwise default(T). Возвращаемый итем</param>
        /// <returns>Operation result. Результат операции</returns>
        public OperationResultEnum GetItem(
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
                        TimeSpan.FromMilliseconds(-1),
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
            //надо завершить уже рабочие триды, которые находятся на любом этапе своего выполнения

            //fire the shutdown event
            //вызываем сигнал остановки потоков
            _stop.Set();

            //do safe dispose
            //и диспозимся когда все триды завершатся
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
                    //сработало внешнее условие прерывания
                    result = OperationResultEnum.ExternalCondition;
                    break;

                case System.Threading.WaitHandle.WaitTimeout:
                    //сработал таймаут ожидания
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
                    throw new InvalidOperationException("Старый итем не был забран, это какая-то ошибка!");
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
                        throw new InvalidOperationException("Новый итем равен null, это какая-то ошибка");
                    }

                    _itemRemoved.Set();
                    result = OperationResultEnum.Success;
                    break;

                case 2:
                    //сработало внешнее условие прерывания
                    result = OperationResultEnum.ExternalCondition;
                    break;

                case System.Threading.WaitHandle.WaitTimeout:
                    //сработал таймаут ожидания
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
            //ИТОГО:
            //рабочие потоки завершены
            //новые потоки не создаются
            //можно утилизироваться

            //dispose all disposable resources
            //утилизируем всё
            _stop.Dispose();
            _itemStored.Dispose();
            _itemRemoved.Dispose();
        }

        #endregion
    }
}