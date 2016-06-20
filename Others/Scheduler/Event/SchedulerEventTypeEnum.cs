namespace Others.Scheduler.Event
{
    public enum SchedulerEventTypeEnum
    {
        /// <summary>
        /// Планировщик стартовал
        /// </summary>
        Started,

        /// <summary>
        /// Планировщик начинает останавливаться
        /// </summary>
        Stopping,

        /// <summary>
        /// Планировщик остановлен
        /// </summary>
        Stopped,

        /// <summary>
        /// Выполнение задачи вызвало ошибку
        /// </summary>
        TaskRaisedException,

        /// <summary>
        /// В планировщике нет задач
        /// </summary>
        NoTask,

        /// <summary>
        /// Начало выполнения задачи
        /// </summary>
        TaskBeginExecution,

        /// <summary>
        /// Конец выполнения задачи
        /// </summary>
        TaskEndExecution,

        /// <summary>
        /// Критическая ошибка работы шедулера, после которой он будет остановлен
        /// </summary>
        CriticalException
    }
}