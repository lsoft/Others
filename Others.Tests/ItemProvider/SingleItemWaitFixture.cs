using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.Disposer;
using Others.ItemProvider;
using Others.ItemProvider.SingleItem;

namespace Others.Tests.ItemProvider
{
    [TestClass]
    public class SingleItemWaitFixture
    {
        private IItemWaitProvider<Item> _itemWaitProvider;

        private readonly ManualResetEvent _threadWorkEvent = new ManualResetEvent(false);

        private Item[][] _datas;
        private long _accumulator = 0L;

        [TestMethod]
        public void AggregationTest()
        {
            var random = new Random(
                int.Parse(Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7), NumberStyles.HexNumber)
                );

            //параметры теста
            var threadCount = Environment.ProcessorCount + 2;
            var itemCount = 100000;
            var maxValue = 100;

            //подготовка данных
            var correctAccumulator = 0L;

            _datas = new Item[threadCount][];
            for (var di = 0; di < threadCount; di++)
            {
                _datas[di] = new Item[itemCount];

                for (var ii = 0; ii < itemCount; ii++)
                {
                    var v = random.Next(maxValue) + 1;
                    
                    _datas[di][ii] = new Item(v);

                    correctAccumulator += v;
                }
            }

            var threads = new Thread[threadCount * 2];
            for (var ti = 0; ti < threads.Length; ti++)
            {
                threads[ti] = new Thread(WorkThread);
            }

            //готовим тестируемый класс
            _itemWaitProvider = new SingleItemWaitProvider<Item>(
                new OptimisticDisposer()
                );

            for (var ti = 0; ti < threads.Length; ti++)
            {
                threads[ti].Start(ti);
            }

            _threadWorkEvent.Set();

            var before = DateTime.Now;

            //все заеблось!

            for (var ti = 0; ti < threads.Length; ti++)
            {
                threads[ti].Join();
            }

            var after = DateTime.Now;
            Debug.WriteLine("Time taken {0}", after - before);

            Assert.AreEqual(correctAccumulator, _accumulator);
        }

        private void WorkThread(object arg)
        {
            //ждем сигнала к началу работу
            _threadWorkEvent.WaitOne();

            var threadIndex = (int)arg;

            if (threadIndex%2 == 0)
            {
                //трид добавления

                var q = _datas[threadIndex / 2];

                for (var ii = 0; ii < q.Length; ii++)
                {
                    _itemWaitProvider.AddItem(q[ii]);
                }
            }
            else
            {
                //трид извлечения

                var localAccumulator = 0L;

                while (true)
                {
                    Item item;
                    var result = _itemWaitProvider.GetItem(
                        TimeSpan.FromSeconds(2),
                        out item
                        );

                    switch (result)
                    {
                        case OperationResultEnum.Success:
                            localAccumulator += item.Value;
                            break;
                        case OperationResultEnum.Timeout:
                            //так организован выход из цикла - если прождали 2 секунды данных - значит тест кончился
                            goto exit;
                        case OperationResultEnum.Dispose:
                            throw new InternalTestFailureException("OperationResultEnum.Dispose");
                        default:
                            throw new ArgumentOutOfRangeException(result.ToString());
                    }
                }

            exit:
                Interlocked.Add(ref _accumulator, localAccumulator);
            }
        }
    }
}
