using System.Threading;

namespace Others.Scheduler.SchedulerThread.Factory
{
    public interface IThreadFactory
    {
        IThread CreateThread(ThreadStart threadStart);
    }
}