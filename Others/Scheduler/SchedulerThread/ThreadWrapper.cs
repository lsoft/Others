using System;
using System.Threading;

namespace Others.Scheduler.SchedulerThread
{
    public class ThreadWrapper : IThread
    {
        private readonly Thread _thread;

        public ThreadWrapper(
            Thread thread
            )
        {
            if (thread == null)
            {
                throw new ArgumentNullException("thread");
            }

            _thread = thread;
        }

        public void Start()
        {
            _thread.Start();
        }

        public void Join()
        {
            _thread.Join();
        }
    }
}