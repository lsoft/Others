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
        /// <summary>
        /// Add new item.
        /// Добавить итем
        /// </summary>
        /// <param name="t">Item. Добавляемый итем</param>
        /// <param name="estimatedCount">Estimated size of the queue after the adding. In case of unsuccessful completion it contains 0.
        /// Its value is ESTIMATED that means it may return approximate size in case of high-load scenario.
        /// Reason of that is ConcurrentQueue does not contain a method like EnqueueAndReturnItsSize as atomic.
        /// Приближенный размер очереди после добавления. В случае неуспешной операции содержит 0.
        /// Значение этой переменной приблизительное в случае высоконагруженного сценария использования провайдера.
        /// Проблема тут в том, что ConcurrentQueue не содержит метода Enqueue, который бы 1) добавлял в очередь 2) сразу возвращал новый размер очереди
        /// как атомарную операцию.
        /// </param>
        /// <returns>Результат операции</returns>
        OperationResultEnum AddItem(
            T t,
            out int estimatedCount
            );

        /// <summary>
        /// Добавить итем
        /// </summary>
        /// <param name="t">Item. Добавляемый итем</param>
        /// <param name="timeout">Operation timeout. Таймаут операции</param>
        /// <param name="estimatedCount">Estimated size of the queue after the adding. In case of unsuccessful completion it contains 0.
        /// Its value is ESTIMATED that means it may return approximate size in case of high-load scenario.
        /// Reason of that is ConcurrentQueue does not contain a method like EnqueueAndReturnItsSize as atomic.
        /// Приближенный размер очереди после добавления. В случае неуспешной операции содержит 0.
        /// Значение этой переменной приблизительное в случае высоконагруженного сценария использования провайдера.
        /// Проблема тут в том, что ConcurrentQueue не содержит метода Enqueue, который бы 1) добавлял в очередь 2) сразу возвращал новый размер очереди
        /// как атомарную операцию.
        /// </param>
        /// <returns>Результат операции</returns>
        OperationResultEnum AddItem(
            T t,
            TimeSpan timeout,
            out int estimatedCount
            );
        
    }
}