using System;

namespace Others.Scheduler.Logger
{
    public interface ISchedulerLogger
    {
        void LogException(Exception excp);
    }
}
