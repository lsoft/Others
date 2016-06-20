using Others.Helper;

namespace Others.Scheduler.WaitGroup
{
    public interface IWaitGroupFactory
    {
        IWaitGroup CreateWaitGroup(
            PerformanceTimer performanceTimer
            );
    }
}