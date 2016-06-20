using System;

namespace Others.Event.Monitor
{
    public class MonitorAutoResetEvent : MonitorBaseResetEvent
    {
        public override MonitorEventTypeEnum Type
        {
            get
            {
                return
                    MonitorEventTypeEnum.Auto;
            }
        }


        public MonitorAutoResetEvent(
            bool signal
            )
            : base(new object(), signal, false)
        {
        }

        protected internal MonitorAutoResetEvent(
            object locker,
            bool signal
            ) : base(locker, signal, true)
        {
        }

        public override void WaitOne()
        {
            if (CreatedToWorkWithinGroup)
            {
                throw new InvalidOperationException("This wait handle has been created to work within a group. WaitOne operation does not support.");
            }

            lock (Locker)
            {
                while (!_signal)
                {
                    System.Threading.Monitor.Wait(Locker);
                }

                _signal = false;
            }
        }

        public override void Set()
        {
            lock (Locker)
            {
                _signal = true;
                System.Threading.Monitor.Pulse(Locker);
            }
        }

        public override void Reset()
        {
            lock (Locker)
            {
                _signal = false;
            }
        }
    }
}