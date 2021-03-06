﻿using System;
using System.Threading;
using Others.Event.Monitor;
using Others.Event.Monitor.Group;
using Others.Helper;

namespace Others.Scheduler.WaitGroup.Monitor
{
    /// <summary>
    /// Ожидатель на Monitor'ах
    /// </summary>
    public class MonitorWaitGroup : IWaitGroup
    {
        private readonly PerformanceTimer _performanceTimer;
        private readonly MonitorEventGroup _waitGroup;

        public MonitorWaitGroup(
            PerformanceTimer performanceTimer
            )
        {
            if (performanceTimer == null)
            {
                throw new ArgumentNullException("performanceTimer");
            }

            _performanceTimer = performanceTimer;
            
            _waitGroup = new MonitorEventGroup(
                MonitorEventTypeEnum.Manual,
                MonitorEventTypeEnum.Auto
                );
        }

        public WaitGroupEventEnum WaitAny(
            long microsecondsToAwake
            )
        {
            //так как данный метод ожидания не может ожидать микросекунды, то конвертируем значение в миллисекунды
            //и повторяем анализ

            long millisecondsToWait;

            if (microsecondsToAwake < 0)
            {
                //отрицательное ожидание означает ждать бесконечно долго
                //значит надо дальше отдать значение -1 миллисекунда, что и означает "ждать бесконечно долго"

                millisecondsToWait = -1;
            }
            else if (microsecondsToAwake == 0)
            {
                //ждать не надо, просто надо проверить сигналы

                millisecondsToWait = 0;
            }
            else
            {
                //ждать надо!

                millisecondsToWait = (microsecondsToAwake - _performanceTimer.MicroSeconds) / 1000L;

                //проверяем получившиеся значение

                if (millisecondsToWait <= 0)
                {
                    //если значение = 0: нас просят ждать слишком малый интервал для этого метода 
                    //если значение < 0: уже прошляпили момент времени старта таски

                    //в любом случае ждать не будем, просто проверим состояние сигналов

                    millisecondsToWait = 0;
                }

                //на всякий случай проверим переполнение
                if (millisecondsToWait > int.MaxValue)
                {
                    throw new NotSupportedException("Слишком длительное ожидание: millisecondsToWait > int.MaxValue");
                }
            }

            var result = _waitGroup.WaitAny(millisecondsToWait);

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
            //nothing to do
        }

        private void RaiseRestartEvent()
        {
            _waitGroup[1].Set();
        }

        private void RaiseStopEvent()
        {
            _waitGroup[0].Set();
        }

    }
}
