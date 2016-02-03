using System;

namespace Others.ItemProvider.Queue
{
    /// <summary>
    /// Item container/provider in a FIFO order. In case of no item - request will wait for a item, timeout or dispose.
    /// Поставщик итемов в порядке очереди. Если итема нет - при запросе он будет ожидать поступления.
    /// При добавлении итема он добавится в конец очереди.
    /// </summary>
    /// <typeparam name="T">Item type. Тип, который хранится в этом контейнере</typeparam>
    public interface IQueueWaitProvider<T>
        : IItemWaitProvider<T>, IDisposable
        where T : class
    {
        
    }
}