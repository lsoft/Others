using System;

namespace Others.Scheduler.WaitGroup
{
    public interface IWaitGroup : IDisposable
    {
        /// <summary>
        /// Ожидать или таймаута или события
        /// </summary>
        /// <param name="microsecondsToAwake">В КАКОЙ МОМЕНТ ВРЕМЕНИ по общему счетчику задача должна сработать; это НЕ задержка до старта!</param>
        /// <returns>Какова причина возврата из ожидания</returns>
        WaitGroupEventEnum WaitAny(
            long microsecondsToAwake
            );

        void RaiseEvent(
            WaitGroupEventEnum eventToRaise
            );
    }
}