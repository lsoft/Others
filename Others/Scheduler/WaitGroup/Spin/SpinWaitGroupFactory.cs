using System;
using Others.Helper;

namespace Others.Scheduler.WaitGroup.Spin
{
    public class SpinWaitGroupFactory : IWaitGroupFactory
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
                new SpinWaitGroup(performanceTimer);
        }
    }
}
