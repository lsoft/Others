using System;
using System.Diagnostics;
using System.Threading;
using Others.Scheduler.SchedulerThread;
using Others.Scheduler.SchedulerThread.Factory;

namespace Others.Tests.PerformanceScheduler.Stuff
{
    /// <summary>
    /// Фабрика потоков с высочайшим приоритетом в ОС.
    /// Используется только для профилирования и тестирования.
    /// В боевых условиях не применять!
    /// </summary>
    internal class HighestPriorityThreadFactory : IThreadFactory
    {
        public IThread CreateThread(ThreadStart threadStart)
        {
            if (threadStart == null)
            {
                throw new ArgumentNullException("threadStart");
            }

            var t = new Thread(
                () =>
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    Thread.BeginThreadAffinity();

                    try
                    {
                        //полезная работа
                        threadStart();
                    }
                    finally
                    {
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                        Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        Thread.EndThreadAffinity();
                    }
                }
                );

            return
                new ThreadWrapper(t);
        }
    }
}