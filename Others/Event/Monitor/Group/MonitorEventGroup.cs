using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Others.Event.Monitor.Group
{
    public class MonitorEventGroup
    {
        private readonly object _locker = new object();

        public IList<MonitorBaseResetEvent> Events
        {
            get;
            private set;
        }

        public MonitorEventGroup(
            params MonitorEventTypeEnum[] types
            )
        {
            var events = new List<MonitorBaseResetEvent>();

            foreach (var t in types)
            {
                MonitorBaseResetEvent r;

                switch (t)
                {
                    case MonitorEventTypeEnum.Manual:
                        r = new MonitorManualResetEvent(_locker, false);
                        break;
                    case MonitorEventTypeEnum.Auto:
                        r = new MonitorAutoResetEvent(_locker, false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                events.Add(r);
            }

            Events = events;
        }


        public void WaitAll(
            )
        {
            lock (_locker)
            {
                while (!EvaluateConditionAll())
                {
                    System.Threading.Monitor.Wait (_locker);
                }

                ResetAll();
            }
        }

        public int WaitAny(
            long millisecondsToWait
            )
        {
            if (millisecondsToWait > int.MaxValue)
            {
                throw new NotSupportedException("Слишком длительное ожидание: millisecondsToWait > int.MaxValue");
            }

            lock (_locker)
            {
                int firedIndex;

                while (!EvaluateConditionAny(out firedIndex))
                {
                    if (!System.Threading.Monitor.Wait(_locker, (int)millisecondsToWait))
                    {
                        return
                            WaitHandle.WaitTimeout;
                    }
                }

                ResetOne(firedIndex);

                return
                    firedIndex;
            }
        }

        public int WaitAny(
            )
        {
            lock (_locker)
            {
                int firedIndex;

                while (!EvaluateConditionAny(out firedIndex))
                {
                    System.Threading.Monitor.Wait (_locker);
                }

                ResetOne(firedIndex);

                return
                    firedIndex;
            }
        }

        public MonitorBaseResetEvent this[int index]
        {
            get
            {
                return Events[index];
            }
        }

        #region private code

        private void ResetAll()
        {
            for (var cc = 0; cc < Events.Count; cc++)
            {
                var e = Events[cc];

                if (e.Type == MonitorEventTypeEnum.Auto)
                {
                    e.Reset();
                }
            }
        }

        private void ResetOne(
            int index
            )
        {
            if (index < 0)
            {
                return;
            }

            var e = Events[index];

            if (e.Type == MonitorEventTypeEnum.Auto)
            {
                e.Reset();
            }
        }

        private bool EvaluateConditionAll(
            )
        {
            return
                Events.All(e => e._signal);
        }

        private bool EvaluateConditionAny(
            out int index
            )
        {
            for (var cc = 0; cc < Events.Count; cc++)
            {
                if (Events[cc]._signal)
                {
                    index = cc;

                    return
                        true;
                }
            }

            index = -1;

            return
                false;
        }

        #endregion

    }
}