using System;

namespace Others.Scheduler.Task
{
    /// <summary>
    /// Таск, который вызывает заданный экшн
    /// </summary>
    public class ActionTask : BaseTask
    {
        private readonly Func<bool> _awakeAction;
        private readonly bool _needToRepeatInCaseOfException;

        /// <summary>
        /// Создать таск.
        /// Этот метод применяется для любых тасков, даже которые надо выполнить на интервале менее 1 МИЛЛИсекунды.
        /// </summary>
        /// <param name="awakeAction">Экшн, который вызывается при срабатывании таска</param>
        /// <param name="needToRepeatInCaseOfException">В случае эксцепшена - надо ли повторять задачу</param>
        /// <param name="timeoutBetweenAwakes">Таймаут, через который задача сработает от момента старта шедулера или, если шедулер уже стартовал> от текущего момента</param>
        public ActionTask(
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            TimeSpan timeoutBetweenAwakes
            )
            : this(Guid.NewGuid(), awakeAction, needToRepeatInCaseOfException, (long)(timeoutBetweenAwakes.TotalMilliseconds * 1000.0))
        {
        }

        /// <summary>
        /// Создать таск.
        /// Этот метод применяется для любых тасков, даже которые надо выполнить на интервале менее 1 МИЛЛИсекунды.
        /// </summary>
        /// <param name="awakeAction">Экшн, который вызывается при срабатывании таска</param>
        /// <param name="needToRepeatInCaseOfException">В случае эксцепшена - надо ли повторять задачу</param>
        /// <param name="microsecondsBetweenAwakes">Количество МИКРОсекунд, через который задача сработает от момента старта шедулера или, если шедулер уже стартовал> от текущего момента</param>
        public ActionTask(
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            long microsecondsBetweenAwakes
            )
            : this(Guid.NewGuid(), awakeAction, needToRepeatInCaseOfException, microsecondsBetweenAwakes)
        {
        }

        /// <summary>
        /// Создать таск.
        /// Этот метод применяется для любых тасков, даже которые надо выполнить на интервале менее 1 МИЛЛИсекунды.
        /// </summary>
        /// <param name="taskGuid">ГУИД добавляемого таска; обычно - Guid.NewGuid()</param>
        /// <param name="awakeAction">Экшн, который вызывается при срабатывании таска</param>
        /// <param name="needToRepeatInCaseOfException">В случае эксцепшена - надо ли повторять задачу</param>
        /// <param name="timeoutBetweenAwakes">Таймаут, через который задача сработает от момента старта шедулера или, если шедулер уже стартовал> от текущего момента</param>
        public ActionTask(
            Guid taskGuid,
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            TimeSpan timeoutBetweenAwakes
            )
            : base(taskGuid, (long)(timeoutBetweenAwakes.TotalMilliseconds * 1000.0))
        {
            if (awakeAction == null)
            {
                throw new ArgumentNullException("awakeAction");
            }

            _awakeAction = awakeAction;
            _needToRepeatInCaseOfException = needToRepeatInCaseOfException;
        }

        /// <summary>
        /// Создать таск.
        /// Этот метод применяется для любых тасков, даже которые надо выполнить на интервале менее 1 МИЛЛИсекунды.
        /// </summary>
        /// <param name="taskGuid">ГУИД добавляемого таска; обычно - Guid.NewGuid()</param>
        /// <param name="awakeAction">Экшн, который вызывается при срабатывании таска</param>
        /// <param name="needToRepeatInCaseOfException">В случае эксцепшена - надо ли повторять задачу</param>
        /// <param name="microsecondsBetweenAwakes">Количество МИКРОсекунд, через который задача сработает от момента старта шедулера или, если шедулер уже стартовал> от текущего момента</param>
        public ActionTask(
            Guid taskGuid,
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            long microsecondsBetweenAwakes
            )
            : base(taskGuid, microsecondsBetweenAwakes)
        {
            if (awakeAction == null)
            {
                throw new ArgumentNullException("awakeAction");
            }

            _awakeAction = awakeAction;
            _needToRepeatInCaseOfException = needToRepeatInCaseOfException;
        }

        public override void Execute(
            out bool needToRepeat
            )
        {
            try
            {
                var r = _awakeAction();

                needToRepeat = r;
            }
            catch
            {
                needToRepeat = _needToRepeatInCaseOfException;

                throw;
            }
        }
    }
}