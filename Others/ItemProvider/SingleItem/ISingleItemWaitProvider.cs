using System;

namespace Others.ItemProvider.SingleItem
{
    /// <summary>
    /// Single item container/provider. In case of no item - request will wait for a item, timeout or dispose.
    /// It'll wait with adding new item in case of one item already contained.
    /// ѕоставщик одного итема. ≈сли итема нет - при запросе он будет ожидать поступлени€.
    /// ≈сли итем есть, то при поступлении нового итема он будет ожидать извлечени€ уже добавленного
    /// итема.
    /// </summary>
    /// <typeparam name="T">Item type. “ип, который хранитс€ в этом контейнере</typeparam>
    public interface ISingleItemWaitProvider<T> 
        : IItemWaitProvider<T>, IDisposable
        where T : class
    {
        
    }
}