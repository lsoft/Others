using System;

namespace Others.Scheduler.Task
{
    public abstract class BaseTask : ITask
    {
        public Guid TaskGuid
        {
            get;
            private set;
        }

        public long MicrosecondsBetweenAwakes
        {
            get;
            private set;
        }

        public BaseTask(
            Guid taskGuid,
            long microsecondsBetweenAwakes
            )
        {

            TaskGuid = taskGuid;
            MicrosecondsBetweenAwakes = microsecondsBetweenAwakes;
        }

        public abstract void Execute(
            out bool needToRepeat
            );
    }
}