using System;

namespace Others.ItemProvider
{
    /// <summary>
    /// Item container/provider. It will wait in case of no item exists during getting a item.
    /// ��������� ������, ������� ������� �������� ������ GetItem, ���� ������ ���
    /// </summary>
    /// <typeparam name="T">��� �����</typeparam>
    public interface IItemWaitProvider<T>
        where T : class
    {
        /// <summary>
        /// Add new item.
        /// �������� ����
        /// </summary>
        /// <param name="t">Item. ����������� ����</param>
        /// <returns>��������� ��������</returns>
        OperationResultEnum AddItem(
            T t
            );

        /// <summary>
        /// �������� ����
        /// </summary>
        /// <param name="t">Item. ����������� ����</param>
        /// <param name="timeout">Operation timeout. ������� ��������</param>
        /// <returns>��������� ��������</returns>
        OperationResultEnum AddItem(
            T t,
            TimeSpan timeout
            );

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// �������� ����. ���� ������ ��� - ����� ������������� �����.
        /// </summary>
        /// <param name="resultItem">Extracted item if success otherwise default(T). ������������ ����</param>
        /// <returns>Operation result. ��������� ��������</returns>
        OperationResultEnum GetItem(
            out T resultItem
            );

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// �������� ����. ���� ������ ��� - ����� �������� �������.
        /// </summary>
        /// <param name="timeout">Operation timeout. ������� ��������</param>
        /// <param name="resultItem">Extracted item if success otherwise default(T). ������������ ����</param>
        /// <returns>Operation result. ��������� ��������</returns>
        OperationResultEnum GetItem(
            TimeSpan timeout,
            out T resultItem
            );
    }
}