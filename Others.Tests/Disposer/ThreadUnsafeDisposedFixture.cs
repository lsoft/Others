using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.Disposer;
using Others.Tests.Helper;

namespace Others.Tests.Disposer
{
    [TestClass]
    public class ThreadUnsafeDisposedFixture
    {
        [TestMethod]
        public void QueuePerformanceTest()
        {
            var stats = new List<TestStat>();

            for (var testIndex = 0; testIndex < 100; testIndex++)
            {
                var queue = new ConcurrentQueue<QItem>();

                var dt = new DisposerTest(
                    new ThreadUnsafeDisposer(),
                    (threadIndex) =>
                    {
                        var q = new QItem(threadIndex);
                        queue.Enqueue(q);
                    },
                    () =>
                    {
                        var q = new QItem(-1);
                        queue.Enqueue(q);
                    }
                    );

                var threadCount = Environment.ProcessorCount + 2;
                var timeout = 50;

                Debug.WriteLine("Test {0} (threads = {1}, timeout = {2})    ", testIndex, threadCount, timeout);

                dt.DoTest(
                    threadCount,
                    timeout
                    );

                //преобразуем в лист

                var resultList = new List<QItem>();

                QItem current;
                while (queue.TryDequeue(out current))
                {
                    resultList.Add(current);
                }

                //не анализируем лист, так как это фейковый диспозер

                //собираем и сохраняем статистику
                var th = new long[threadCount];
                foreach (var i in resultList)
                {
                    if (i.ThreadId >= 0)
                    {
                        th[i.ThreadId]++;
                    }
                }

                var stat = new TestStat(
                    th,
                    resultList.Count - 1 // - 1 это вычесть евент диспоуза
                    );

                stats.Add(stat);

                GC.Collect(3);
                GC.WaitForPendingFinalizers();
                GC.Collect(3);
            }

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("Success");

            Debug.WriteLine("Total = {0}", stats.Sum(j => j.TotalCount));
        }

        [TestMethod]
        public void NothingToDoPerformanceTest()
        {
            var hits = new[] {0L};

            for (var testIndex = 0; testIndex < 100; testIndex++)
            {
                var dt = new DisposerTest(
                    new ThreadUnsafeDisposer(),
                    (threadIndex) =>
                    {
                        Interlocked.Increment(ref hits[0]);
                    },
                    () =>
                    {
                        //ничего не делаем - это тест на производительность
                    }
                    );

                var threadCount = Environment.ProcessorCount + 2;
                var timeout = 50;

                Debug.WriteLine("Test {0} (threads = {1}, timeout = {2})    ", testIndex, threadCount, timeout);

                dt.DoTest(
                    threadCount,
                    timeout
                    );

                GC.Collect(3);
                GC.WaitForPendingFinalizers();
                GC.Collect(3);
            }

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("Success");

            Debug.WriteLine("Total = {0}", hits[0]);
        }
    }
}