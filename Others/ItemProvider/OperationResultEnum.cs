namespace Others.ItemProvider
{
    /// <summary>
    /// Operation result.
    /// Результат ожидания итема.
    /// </summary>
    public enum OperationResultEnum
    {
        /// <summary>
        /// Operation finished successfully.
        /// Операция выполнена успешно.
        /// </summary>
        Success,

        /// <summary>
        /// Operation cancelled due to timeout.
        /// Операция не выполнено из-за таймаута ожидания.
        /// </summary>
        Timeout,

        /// <summary>
        /// Operation cancelled due to dispose is performing.
        /// Операция не завершена, так как класс получил приказ утилизироваться.
        /// </summary>
        Dispose,

        /// <summary>
        /// Operation cancelled due to external event.
        /// Операция прервана по внешнему событию
        /// </summary>
        ExternalCondition
    }
}