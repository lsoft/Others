namespace Others.Tests.Disposer.Stuff
{
    public class TestStat
    {
        public long[] CountByThread
        {
            get;
            private set;
        }

        public long TotalCount
        {
            get;
            private set;
        }

        public TestStat(long[] countByThread, long totalCount)
        {
            CountByThread = countByThread;
            TotalCount = totalCount;
        }
    }
}