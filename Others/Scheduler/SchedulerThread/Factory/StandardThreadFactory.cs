using System.Threading;

namespace Others.Scheduler.SchedulerThread.Factory
{
    public class StandardThreadFactory : IThreadFactory
    {
        public IThread CreateThread(ThreadStart threadStart)
        {
            var thread = new Thread(threadStart);

            return 
                new ThreadWrapper(thread);
        }
    }
}
