using System.Threading;

namespace Others.Tests.Disposer.Stuff
{
    public class QItem
    {
        private static long _index = 0L;

        public int ThreadId
        {
            get;
            private set;
        }

        public long Counter
        {
            get;
            private set;
        }

        public QItem(
            int threadId
            )
        {
            ThreadId = threadId;
            Counter = Interlocked.Increment(ref _index);
        }
    }
}