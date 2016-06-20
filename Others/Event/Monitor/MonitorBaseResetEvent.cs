using System;

namespace Others.Event.Monitor
{
    public abstract class MonitorBaseResetEvent
    {
        protected readonly object Locker;
        protected internal bool _signal;
        protected readonly bool CreatedToWorkWithinGroup;

        public abstract MonitorEventTypeEnum Type
        {
            get;
        }

        protected MonitorBaseResetEvent(
            object locker,
            bool signal,
            bool createdToWorkWithinGroup
            )
        {
            if (locker == null)
            {
                throw new ArgumentNullException("locker");
            }

            Locker = locker;
            _signal = signal;
            CreatedToWorkWithinGroup = createdToWorkWithinGroup;
        }

        public abstract void WaitOne();
        public abstract void Set();
        public abstract void Reset();
    }
}