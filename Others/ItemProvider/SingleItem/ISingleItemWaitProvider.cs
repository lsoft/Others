using System;

namespace Others.ItemProvider.SingleItem
{
    /// <summary>
    /// Single item container/provider. In case of no item - request will wait for a item, timeout or dispose.
    /// It'll wait with adding new item in case of one item already contained.
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