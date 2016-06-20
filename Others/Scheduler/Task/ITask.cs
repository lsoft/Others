using System;

namespace Others.Scheduler.Task
{
    public interface ITask
    {
        Guid TaskGuid
        {
            get;
        }

        long MicrosecondsBetweenAwakes
        {
            get;
        }

        void Execute(
            out bool needToRepeat
            );
    }
}