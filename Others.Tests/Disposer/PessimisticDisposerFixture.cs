using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.Disposer;
using Others.Tests.Helper;

namespace Others.Tests.Disposer
{
    [TestClass]
    public class PessimisticDisposerFixture
    {
        [TestMethod]
        public void QueueCorrectnessTest()
        {
            var random = new Random(
                int.Parse(Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7), NumberStyles.HexNumber)
                );


            for (var testIndex = 0; testIndex < 150; testIndex++)
            {
                var queue = new ConcurrentQueue<QItem>();

                var dt = new DisposerTest(
                    new PessimisticDisposer(),
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

                var threadCount = 2 + random.Next(Environment.ProcessorCount*2);
                var timeout = 5 + random.Next(20);

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

                //анализируем лист

                //проверяем что диспоуз 1
                if (resultList.Count(j => j.ThreadId == -1) != 1)
                {
                    throw new InvalidOperationException("диспоузов несколько");
                }

                //проверяем что диспоуз последний
                if (resultList.Last().ThreadId != -1)
                {
                    throw new InvalidOperationException("диспоуз не последний");
                }

                GC.Collect(3);
                GC.WaitForPendingFinalizers();
                GC.Collect(3);
            }

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("Success");
        }

        [TestMethod]
        public void CounterCorrectnessTest()
        {
            var random = new Random(
                int.Parse(Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7), NumberStyles.HexNumber)
                );


            for (var testIndex = 0; testIndex < 150; testIndex++)
            {
                long[] disposeCount = {0L};

                var dt = new DisposerTest(
                    new PessimisticDisposer(),
                    (threadIndex) =>
                    {
                        if (Interlocked.Read(ref disposeCount[0]) > 0L)
                        {
                            throw new InternalTestFailureException("Диспоуз прошел, а работа продолжается");
                        }
                    },
                    () =>
                    {
                        Interlocked.Increment(ref disposeCount[0]);
                    }
                    );

                var threadCount = 2 + random.Next(Environment.ProcessorCount*2);
                var timeout = 5 + random.Next(20);

                Debug.WriteLine("Test {0} (threads = {1}, timeout = {2})    ", testIndex, threadCount, timeout);

                dt.DoTest(
                    threadCount,
                    timeout
                    );

                //анализируем результат

                //проверяем что диспоуз 1
                if (Interlocked.Read(ref disposeCount[0]) != 1)
                {
                    throw new InvalidOperationException("диспоузов несколько");
                }

                //проверяем что диспоуз последний
                //проверять не надо, проверяется внутри workAction (ищи InternalTestFailureException)

                GC.Collect(3);
                GC.WaitForPendingFinalizers();
                GC.Collect(3);
            }

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("Success");
        }

        [TestMethod]
        public void QueuePerformanceTest()
        {
            var stats = new List<TestStat>();

            for (var testIndex = 0; testIndex < 100; testIndex++)
            {
                var queue = new ConcurrentQueue<QItem>();

                var dt = new DisposerTest(
                    new PessimisticDisposer(),
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

                //анализируем лист

                //проверяем что диспоуз 1
                if (resultList.Count(j => j.ThreadId == -1) != 1)
                {
                    throw new InvalidOperationException("диспоузов несколько");
                }

                //проверяем что диспоуз последний
                if (resultList.Last().ThreadId != -1)
                {
                    throw new InvalidOperationException("диспоуз не последний");
                }

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
                    new PessimisticDisposer(),
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