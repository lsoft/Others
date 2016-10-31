using System;
using System.Threading;
using Others.Helper;

namespace Others.Scheduler.WaitGroup.Spin
{
    /// <summary>
    /// Ожидатель на SpinWait
    /// </summary>
    public class SpinWaitGroup : IWaitGroup
    {
        private readonly PerformanceTimer _performanceTimer;

        private long _stopRaised;
        private long _restartRaised;

        public SpinWaitGroup(
            PerformanceTimer performanceTimer
            )
        {
            if (performanceTimer == null)
            {
                throw new ArgumentNullException("performanceTimer");
            }

            _performanceTimer = performanceTimer;
        }

        public WaitGroupEventEnum WaitAny(
            long microsecondsToAwake
            )
        {
            var result = WaitGroupEventEnum.WaitTimeout;

            var spinWait = new SpinWait();

            while (true)
            {
                var stopRaised = Interlocked.Read(ref _stopRaised);
                if (stopRaised > 0L)
                {
                    //stopRaised - is a 'manual' event
                    //no decrement at all!

                    result = WaitGroupEventEnum.Stop;

                    break;
                }

                //restartRaised - is an 'auto' event , so we need to 'reset' the event
                var restartRaised = Interlocked.Exchange(ref _restartRaised, 0L);
                if (restartRaised > 0L)
                {
                    result = WaitGroupEventEnum.Restart;

                    break;
                }

                //если ждать не надо, сразу выходим
                if (microsecondsToAwake == 0)
                {
                    break;
                }

                //проверяем не истек ли таймаут только когда таймаут задан
                if (microsecondsToAwake > 0)
                {
                    if (microsecondsToAwake <= _performanceTimer.MicroSeconds)
                    {
                        //сработал таймаут
                        break;
                    }
                }

                spinWait.SpinOnce();
            }

            return
                result;
        }

        public void RaiseEvent(WaitGroupEventEnum eventToRaise)
        {
            switch (eventToRaise)
            {
                case WaitGroupEventEnum.Restart:
                    RaiseRestartEvent();
                    break;
                case WaitGroupEventEnum.Stop:
                    RaiseStopEvent();
                    break;
                case WaitGroupEventEnum.WaitTimeout:
                    throw new InvalidOperationException("RaiseEvent: WaitGroupEventEnum.WaitTimeout");
                default:
                    throw new ArgumentOutOfRangeException("eventToRaise");
            }
        }

        public void Dispose()
        {
            //nothing to do
        }


        private void RaiseRestartEvent()
        {
            Interlocked.Exchange(ref _restartRaised, 1L);
        }

        private void RaiseStopEvent()
        {
            Interlocked.Exchange(ref _stopRaised, 1L);
        }

    }
}
