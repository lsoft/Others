using System;
using System.Threading;
using Others.Disposer;

namespace Others.ItemProvider.SingleItem
{
    /// <summary>
    /// Single item container/provider. In case of no item - request will wait for a item, timeout or dispose.
    /// In case of an item is already contained new adding request will wait.
    /// ��������� ������ �����. ���� ����� ��� - ��� ������� �� ����� ������� �����������.
    /// ���� ���� ����, �� ��� ����������� ������ ����� �� ����� ������� ���������� ��� ������������
    /// �����.
    /// </summary>
    /// <typeparam name="T">���, ������� �������� � ���� ����������</typeparam>
    public class SingleItemWaitProvider<T>
        : ISingleItemWaitProvider<T>
        where T  : class
    {

        /// <summary>
        /// Store item event.
        /// ����� ��� ����� ���� ��������; �� ������ ������������� ������������ ����� ����������
        /// </summary>
        private readonly AutoResetEvent _itemStored = new AutoResetEvent(false);

        /// <summary>
        /// Extract item event.
        /// ����� ��� ����� ���� ��������; �� ������ ������������� ������������ ����� ����������
        /// </summary>
        private readonly AutoResetEvent _itemRemoved = new AutoResetEvent(true);

        /// <summary>
        /// Shutdown (stop working) event.
        /// ����� ���������� ������; ��� ������������� ���������� �� ����
        /// </summary>
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);

        /// <summary>
        /// Dispose guard.
        /// ������, ���������� ����������� ������ � ������� ���������������
        /// </summary>
        private readonly IThreadSafeDisposer _disposer;

        /// <summary>
        /// Item to be stored.
        /// ��������, ������� ������ ���������
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
            //���� ��������� ��� ������� �����, ������� ��������� �� ����� ����� ������ ����������

            //fire the shutdown event
            //�������� ������ ��������� �������
            _stop.Set();

            //do safe dispose
            //� ���������� ����� ��� ����� ����������
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

            //�������, ��� ����������
            var r = WaitHandle.WaitAny(
                GetRemovedWaitHandles(externalBreakHandle),
                timeout
                );

            switch (r)
            {
                case 0:
                    //�������� ����-�����
                    //��������� ����
                    result = OperationResultEnum.Dispose;
                    break;

                case 1:
                    //�������� �����, ��� ���� ��������
                    result = OperationResultEnum.Success;
                    break;

                case 2:
                    //��������� ������� ������� ����������
                    result = OperationResultEnum.ExternalCondition;
                    break;

                case System.Threading.WaitHandle.WaitTimeout:
                    //�������� ������� ��������
                    result = OperationResultEnum.Timeout;
                    break;

                default:
                    //�������������� �������
                    throw new InvalidOperationException(r.ToString());
            }


            if (result == OperationResultEnum.Success)
            {
                var oldValue = Interlocked.Exchange(ref _value, t);

                if (oldValue != null)
                {
                    throw new InvalidOperationException("������ ���� �� ��� ������, ��� �����-�� ������!");
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

            //�������, ��� ����������
            var r = WaitHandle.WaitAny(
                GetStoredWaitHandles(externalBreakHandle),
                timeout
                );

            OperationResultEnum result;
            resultItem = default(T);

            switch (r)
            {
                case 0:
                    //�������� ����-�����
                    result = OperationResultEnum.Dispose;
                    break;

                case 1:
                    //�������� ����� "���� ��������"
                    resultItem = Interlocked.Exchange(ref _value, null);

                    if (resultItem == null)
                    {
                        throw new InvalidOperationException("����� ���� ����� null, ��� �����-�� ������");
                    }

                    _itemRemoved.Set();
                    result = OperationResultEnum.Success;
                    break;

                case 2:
                    //��������� ������� ������� ����������
                    result = OperationResultEnum.ExternalCondition;
                    break;

                case System.Threading.WaitHandle.WaitTimeout:
                    //�������� ������� ��������
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
            //�����:
            //������� ������ ���������
            //����� ������ �� ���������
            //����� ���������������

            //dispose all disposable resources
            //����������� ��
            _stop.Dispose();
            _itemStored.Dispose();
            _itemRemoved.Dispose();
        }

        #endregion
    }
}