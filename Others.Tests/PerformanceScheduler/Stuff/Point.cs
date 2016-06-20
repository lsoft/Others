namespace Others.Tests.PerformanceScheduler.Stuff
{
    public class Point
    {
        public long Left
        {
            get;
            private set;
        }

        public long Right
        {
            get;
            private set;
        }

        public long Diff
        {
            get;
            private set;
        }

        public Point(long left, long right)
        {
            Left = left;
            Right = right;
            Diff = Right - Left;
        }
    }
}
