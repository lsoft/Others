using System;
using System.Threading;

namespace Others.ItemProvider
{
    /// <summary>
    /// Item container/provider. It will wait in case of no item exists during getting a item.
    /// Поставщик итемов, который создает ожидание внутри GetItem, если итемов нет
    /// </summary>
    /// <typeparam name="T">Тип итема</typeparam>
    public interface IItemWaitProvider<T>
        where T : class
    {
        /// <summary>
        /// Add new item.
        /// Добавить итем
        /// </summary>
        /// <param name="t">Item. Добавляемый итем</param>
        /// <returns>Результат операции</returns>
        OperationResultEnum AddItem(
            T t
            );

        /// <summary>
        /// Добавить итем
        /// </summary>
        /// <param name="t">Item. Добавляемый итем</param>
        /// <param name="timeout">Operation timeout. Таймаут операции</param>
        /// <returns>Результат операции</returns>
        OperationResultEnum AddItem(
            T t,
            TimeSpan timeout
            );

        /// <summary>
        /// Добавить итем
        /// </summary>
        /// <param name="t">Item. Добавляемый итем</param>
        /// <param name="timeout">Operation timeout. Таймаут операции</param>
        /// <param name="externalBreakHandle">External break handle. Внешний хендл прерывания ожидания</param>
        /// <returns>Результат операции</returns>
        OperationResultEnum AddItem(
            T t,
            TimeSpan timeout,
            WaitHandle externalBreakHandle
            );

        /// <summary>
        /// Добавить итем
        /// </summary>
        /// <param name="t">Item. Добавляемый итем</param>
        /// <param name="externalBreakHandle">External break handle. Внешний хендл прерывания ожидания</param>
        /// <returns>Результат операции</returns>
        OperationResultEnum AddItem(
            T t,
            WaitHandle externalBreakHandle
            );

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// Получить итем. Если итемов нет - ждать неопределенно долго.
        /// </summary>
        /// <param name="resultItem">Extracted item if success otherwise default(T). Возвращаемый итем</param>
        /// <returns>Operation result. Результат операции</returns>
        OperationResultEnum GetItem(
            out T resultItem
            );

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// Получить итем. Если итемов нет - ждать указаный таймаут.
        /// </summary>
        /// <param name="timeout">Operation timeout. Таймаут операции</param>
        /// <param name="resultItem">Extracted item if success otherwise default(T). Возвращаемый итем</param>
        /// <returns>Operation result. Результат операции</returns>
        OperationResultEnum GetItem(
            TimeSpan timeout,
            out T resultItem
            );

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// Получить итем. Если итемов нет - ждать указаный таймаут.
        /// </summary>
        /// <param name="externalBreakHandle">External break handle. Внешний хендл прерывания ожидания</param>
        /// <param name="resultItem">Extracted item if success otherwise default(T). Возвращаемый итем</param>
        /// <returns>Operation result. Результат операции</returns>
        OperationResultEnum GetItem(
            WaitHandle externalBreakHandle,
            out T resultItem
            );

        /// <summary>
        /// Get item from the container. It will wait in case of no item was stored.
        /// Получить итем. Если итемов нет - ждать указаный таймаут.
        /// </summary>
        /// <param name="timeout">Operation timeout. Таймаут операции</param>
        /// <param name="externalBreakHandle">External break handle. Внешний хендл прерывания ожидания</param>
        /// <param name="resultItem">Extracted item if success otherwise default(T). Возвращаемый итем</param>
        /// <returns>Operation result. Результат операции</returns>
        OperationResultEnum GetItem(
            TimeSpan timeout,
            WaitHandle externalBreakHandle,
            out T resultItem
            );
    }
}