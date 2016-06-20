using System;
using System.Threading;
using Others.Helper;

namespace Others.Scheduler.WaitGroup.Standard
{
    /// <summary>
    /// Ожидатель на WaitHandl'ах
    /// </summary>
    public class StandardWaitGroup : IWaitGroup
    {
        private readonly PerformanceTimer _performanceTimer;

        private readonly ManualResetEvent _stop = new ManualResetEvent(false);
        private readonly AutoResetEvent _restart = new AutoResetEvent(false);

        private readonly WaitHandle[] _events;

        private bool _disposed = false;

        public StandardWaitGroup(
            PerformanceTimer performanceTimer
            )
        {
            if (performanceTimer == null)
            {
                throw new ArgumentNullException("performanceTimer");
            }

            _performanceTimer = performanceTimer;

            _events = new WaitHandle[]
            {
                _stop,
                _restart,
            };
        }

        public WaitGroupEventEnum WaitAny(
            long microsecondsToAwake
            )
        {
            //ожидать не надо, так как время ожидания указано равным нулю
            if (microsecondsToAwake == 0)
            {
                return
                    WaitGroupEventEnum.WaitTimeout;
            }

            //так как данный метод ожидания не может ожидать микросекунды, то конвертируем значение в миллисекунды
            //и повторяем анализ

            long millisecondsToWait;

            if (microsecondsToAwake < 0)
            {
                //отрицательное ожидание означает ждать бесконечно долго
                //значит надо дальше отдать значение -1 миллисекунда, что и означает "ждать бесконечно долго"

                millisecondsToWait = -1;
            }
            else
            {
                //ждать надо!

                //сколько ждать в миллисекундах?
                millisecondsToWait = (microsecondsToAwake - _performanceTimer.MicroSeconds) / 1000L;

                //проверяем получившиеся значение

                if (millisecondsToWait <= 0)
                {
                    //если значение = 0: нас просят ждать слишком малый интервал для этого метода 
                    //если значение < 0: уже прошляпили момент времени старта таски

                    //в любом случае ждать не надо

                    return
                        WaitGroupEventEnum.WaitTimeout;
                }

                //на всякий случай проверим переполнение
                if (millisecondsToWait > int.MaxValue)
                {
                    throw new NotSupportedException("Слишком длительное ожидание: millisecondsToWait > int.MaxValue");
                }
            }

            var result = WaitHandle.WaitAny(_events, (int)millisecondsToWait);

            switch (result)
            {
                case 0:
                    return WaitGroupEventEnum.Stop;
                case 1:
                    return WaitGroupEventEnum.Restart;
                case WaitHandle.WaitTimeout:
                    return WaitGroupEventEnum.WaitTimeout;
                default:
                    throw new NotSupportedException(result.ToString());
            }
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
            if (!_disposed)
            {
                _stop.Dispose();
                _restart.Dispose();

                _disposed = true;
            }
        }

        private void RaiseRestartEvent()
        {
            _restart.Set();
        }

        private void RaiseStopEvent()
        {
            _stop.Set();
        }

    }
}