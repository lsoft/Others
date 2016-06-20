using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.Disposer;
using Others.ItemProvider.Queue;
using Others.Tests.ItemProvider.Stuff;

namespace Others.Tests.ItemProvider
{
    [TestClass]
    public class MonitorWaitProviderFixture
    {
        [TestMethod]
        public void AggregationTest()
        {
            //готовим тестируемый класс
            var itemWaitProvider =
                new MonitorWaitProvider<Item>(
                    new ThreadUnsafeDisposer()
                    );

            var qf = new QueueFixture(
                itemWaitProvider
                );

            qf.AggregationTest();
        }

    }
}
