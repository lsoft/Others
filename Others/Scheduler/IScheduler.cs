using System;
using Others.Scheduler.Event;
using Others.Scheduler.Task;

namespace Others.Scheduler
{
    /// <summary>
    /// Планировщик задач
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// События, генерируемые шедулером.
        /// Обработчики этих событий не должно занимать длительно время,
        /// так как событие вызывается из рабочих потоков шедулера.
        /// Идеальный вариант - быстро сохранять событие в очередь обработки и возвращать управление,
        /// чтобы не держать сам шедулер, а очередь разбирать в отдельном потоке.
        /// </summary>
        event SchedulerEventDelegate SchedulerEvent;

        /// <summary>
        /// Текущее количество тасков в шедулере
        /// </summary>
        int TaskCount
        {
            get;
        }

        /// <summary>
        /// Старт шедулера.
        /// </summary>
        void Start(
            );

        /// <summary>
        /// Добавить таск в шедулер.
        /// </summary>
        void AddTask(
            ITask task
            );

        /// <summary>
        /// Отменить ожидающую задачу.
        /// Если задача уже начала выполняться, то ее выполнение не будет прервано.
        /// </summary>
        /// <param name="taskGuid">ГУИД задачи</param>
        void CancelTask(
            Guid taskGuid
            );
    }
}