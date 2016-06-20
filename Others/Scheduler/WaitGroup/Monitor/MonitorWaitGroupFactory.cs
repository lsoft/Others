using System;
using Others.Helper;

namespace Others.Scheduler.WaitGroup.Monitor
{
    public class MonitorWaitGroupFactory : IWaitGroupFactory
    {
        public IWaitGroup CreateWaitGroup(
            PerformanceTimer performanceTimer
            )
        {
            if (performanceTimer == null)
            {
                throw new ArgumentNullException("performanceTimer");
            }

            return
                new MonitorWaitGroup(performanceTimer);
        }
    }
}
