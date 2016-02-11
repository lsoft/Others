using System;

namespace Others.ItemProvider.SingleItem
{
    /// <summary>
    /// Single item container/provider. In case of no item - request will wait for a item, timeout or dispose.
    /// In case of an item is already contained new adding request will wait.
    /// ��������� ������ �����. ���� ����� ��� - ��� ������� �� ����� ������� �����������.
    /// ���� ���� ����, �� ��� ����������� ������ ����� �� ����� ������� ���������� ��� ������������
    /// �����.
    /// </summary>
    /// <typeparam name="T">Item type. ���, ������� �������� � ���� ����������</typeparam>
    public interface ISingleItemWaitProvider<T> 
        : IItemWaitProvider<T>, IDisposable
        where T : class
    {
        
    }
}