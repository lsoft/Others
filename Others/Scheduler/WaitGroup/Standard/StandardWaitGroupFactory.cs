using System;
using Others.Helper;

namespace Others.Scheduler.WaitGroup.Standard
{
    public class StandardWaitGroupFactory : IWaitGroupFactory
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
                new StandardWaitGroup(
                    performanceTimer
                    );
        }
    }
}
