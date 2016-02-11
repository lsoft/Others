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
        private readonly AutoResetEvent _itemStoredEvent = new AutoResetEvent(false);

        /// <summary>
        /// Extract item event.
        /// ����� ��� ����� ���� ��������; �� ������ ������������� ������������ ����� ����������
        /// </summary>
        private readonly AutoResetEvent _itemRemovedEvent = new AutoResetEvent(true);

        /// <summary>
        /// Shutdown (stop working) event.
        /// ����� ���������� ������; ��� ������������� ���������� �� ����
        /// </summary>
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

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
        
        /// <summary>
        /// Wait handles array used in removing items.
        /// ������ ����������, ����� �� ��������� ��� ������ ��� ������ DoAddItem
        /// </summary>
        private readonly WaitHandle[] _stopRemovedWaiters;

        /// <summary>
        /// Wait handles array used in adding items.
        /// ������ ����������, ����� �� ��������� ��� ������ ��� ������ DoGetItem
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
            //���� ��������� ��� ������� �����, ������� ��������� �� ����� ����� ������ ����������

            //fire the shutdown event
            //�������� ������ ��������� �������
            _stopEvent.Set();

            //do safe dispose
            //� ���������� ����� ��� ����� ����������
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

            //�������, ��� ����������
            var r = WaitHandle.WaitAny(
                _stopRemovedWaiters,
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
            //�������, ��� ����������
            var r = WaitHandle.WaitAny(
                _stopStoredWaiters,
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

                    _itemRemovedEvent.Set();
                    result = OperationResultEnum.Success;
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
            _stopEvent.Dispose();
            _itemStoredEvent.Dispose();
            _itemRemovedEvent.Dispose();
        }

        #endregion
    }
}