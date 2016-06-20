using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.Scheduler;
using Others.Scheduler.Logger;
using Others.Scheduler.WaitGroup;
using Others.Scheduler.WaitGroup.Monitor;
using Others.Scheduler.WaitGroup.Spin;
using Others.Scheduler.WaitGroup.Standard;
using Others.Tests.PerformanceScheduler.Stuff;

namespace Others.Tests.PerformanceScheduler
{
    [TestClass]
    public class SchedulerFixture
    {
        private const long MicrosecondsInSecond = 1000000;
        private const int SkipItemCount = 300;

        private const string PointsFailMessage = "### Все события отфильтрованы как выбросы, это явная ошибка, но нельзя заранее сказать где ###";
        private const string MeanFailMessage = "### Очень непохожая медиана на ту, что требуется ###";
        private const string StdFailMessage = "### Сильное стандартное отклонение, метод ожидания очень неточный ###";

        [TestMethod]
        public void FewMicroSecondsIntervalTest()
        {

            const int TaskCount = 2500000;
            const long TaskDiscretMicroSeconds = 5;
            const long JetMicroseconds = 1000; //1 millisec

            var times = new long[TaskCount];

            var wgf = new SpinWaitGroupFactory(
                );

            using (var scheduler = CreateScheduler(wgf))
            {
                //добавляем периодический таск

                var task = new TestTask(
                    TaskCount,
                    TaskDiscretMicroSeconds,
                    times
                    );

                scheduler.AddTask(task);

                //стартуем шедулер
                scheduler.Start();

                //ждем пока таски не кончатся
                while (scheduler.TaskCount > 0)
                {
                    Thread.Sleep(1000);
                }

                //шедулер отработал
            }

            //считаем дифф, отбрасывая первые SkipItemCount событий, чтобы пропустить "переходные процессы"
            var jetCount = 0L;
            
            var points = GenerateDiffs(
                SkipItemCount,
                times,
                JetMicroseconds,
                out jetCount
                );

            //считаем статистические параметры

            bool pointsOk;
            bool meanOk;
            bool stdOk;

            ProcessDiffs(
                points,
                TaskDiscretMicroSeconds,
                jetCount,
                out pointsOk,
                out meanOk,
                out stdOk
                );

            //выполняем проверку условий теста

            Assert.IsTrue(
                pointsOk,
                PointsFailMessage
                );
            Assert.IsTrue(
                meanOk,
                MeanFailMessage
                );
            Assert.IsTrue(
                stdOk,
                StdFailMessage
                );
        }
        
        [TestMethod]
        public void TensOfMicroSecondIntervalTest()
        {
            const int TaskCount = 120000;
            const long TaskDiscretMicroSeconds = 30;
            const long JetMicroseconds = 5000; //5 millisec

            var times = new long[TaskCount];

            var wgf = new SpinWaitGroupFactory(
                );


            using (var scheduler = CreateScheduler(wgf))
            {
                //добавляем периодический таск

                var task = new TestTask(
                    TaskCount,
                    TaskDiscretMicroSeconds,
                    times
                    );

                scheduler.AddTask(task);

                //стартуем шедулер
                scheduler.Start();

                //ждем пока таски не кончатся
                while (scheduler.TaskCount > 0)
                {
                    Thread.Sleep(1000);
                }

                //шедулер отработал
            }

            //считаем дифф, отбрасывая первые SkipItemCount событий, чтобы пропустить "переходные процессы"
            var jetCount = 0L;

            var points = GenerateDiffs(
                SkipItemCount,
                times,
                JetMicroseconds,
                out jetCount
                );

            //считаем статистические параметры

            bool pointsOk;
            bool meanOk;
            bool stdOk;

            ProcessDiffs(
                points,
                TaskDiscretMicroSeconds,
                jetCount,
                out pointsOk,
                out meanOk,
                out stdOk
                );

            //выполняем проверку условий теста

            Assert.IsTrue(
                pointsOk,
                PointsFailMessage
                );
            Assert.IsTrue(
                meanOk,
                MeanFailMessage
                );
            Assert.IsTrue(
                stdOk,
                StdFailMessage
                );
        }

        [TestMethod]
        public void MonitorWaitGroup_HundredsOfMicroSecondIntervalTest()
        {
            const int TaskCount = 12000;
            const long TaskDiscretMicroSeconds = 850;
            const long JetMicroseconds = 10 * TaskDiscretMicroSeconds;

            var times = new long[TaskCount];

            var wgf = new MonitorWaitGroupFactory(
                );

            using (var scheduler = CreateScheduler(wgf))
            {
                //добавляем периодический таск

                var task = new TestTask(
                    TaskCount,
                    TaskDiscretMicroSeconds,
                    times
                    );

                scheduler.AddTask(task);

                //стартуем шедулер
                scheduler.Start();

                //ждем пока таски не кончатся
                while (scheduler.TaskCount > 0)
                {
                    Thread.Sleep(1000);
                }

                //шедулер отработал
            }

            //считаем дифф, отбрасывая первые SkipItemCount событий, чтобы пропустить "переходные процессы"
            var jetCount = 0L;

            var points = GenerateDiffs(
                SkipItemCount,
                times,
                JetMicroseconds,
                out jetCount
                );

            //считаем статистические параметры

            bool pointsOk;
            bool meanOk;
            bool stdOk;

            ProcessDiffs(
                points,
                TaskDiscretMicroSeconds,
                jetCount,
                out pointsOk,
                out meanOk,
                out stdOk
                );

            //выполняем проверку условий теста

            Assert.IsTrue(
                pointsOk,
                PointsFailMessage
                );
            Assert.IsTrue(
                meanOk,
                MeanFailMessage
                );
            Assert.IsTrue(
                stdOk,
                StdFailMessage
                );
        }

        [TestMethod]
        public void StandardWaitGroup_HundredsOfMicroSecondIntervalTest()
        {
            const int TaskCount = 12000;
            const long TaskDiscretMicroSeconds = 850;
            const long JetMicroseconds = 10 * TaskDiscretMicroSeconds;

            var times = new long[TaskCount];

            var wgf = new StandardWaitGroupFactory(
                );

            using (var scheduler = CreateScheduler(wgf))
            {
                //добавляем периодический таск

                var task = new TestTask(
                    TaskCount,
                    TaskDiscretMicroSeconds,
                    times
                    );

                scheduler.AddTask(task);

                //стартуем шедулер
                scheduler.Start();

                //ждем пока таски не кончатся
                while (scheduler.TaskCount > 0)
                {
                    Thread.Sleep(1000);
                }

                //шедулер отработал
            }

            //считаем дифф, отбрасывая первые SkipItemCount событий, чтобы пропустить "переходные процессы"
            var jetCount = 0L;

            var points = GenerateDiffs(
                SkipItemCount,
                times,
                JetMicroseconds,
                out jetCount
                );

            //считаем статистические параметры

            bool pointsOk;
            bool meanOk;
            bool stdOk;

            ProcessDiffs(
                points,
                TaskDiscretMicroSeconds,
                jetCount,
                out pointsOk,
                out meanOk,
                out stdOk
                );

            //выполняем проверку условий теста

            Assert.IsTrue(
                pointsOk,
                PointsFailMessage
                );
            Assert.IsTrue(
                meanOk,
                MeanFailMessage
                );
            Assert.IsTrue(
                stdOk,
                StdFailMessage
                );
        }

        #region private helper code

        private static void ProcessDiffs(
            IList<Point> points,
            long taskDiscretMicroSeconds,
            long jetCount,
            out bool pointsOk,
            out bool meanOk,
            out bool stdOk
            )
        {
            var mean = points.Mean();
            var standardDeviation = points.StandardDeviation();

            //выводим результат

            Console.WriteLine(
                "Task discret: {0} microsec",
                taskDiscretMicroSeconds
                );
            Console.WriteLine(string.Empty);

            Console.WriteLine(
                "Mean: {0} microsec",
                mean
                );
            Console.WriteLine(
                "Standard deviation: {0} microsec",
                standardDeviation
                );
            Console.WriteLine(
                "Jet count removed: {0}",
                jetCount
                );
            Console.WriteLine(string.Empty);


            //выводим условия провала

            pointsOk = true;
            if (points.Count == 0)
            {
                Console.WriteLine(PointsFailMessage);
                pointsOk = false;
            }

            meanOk = true;
            if (Math.Abs(mean).NotInRange(0.9*taskDiscretMicroSeconds, 1.1*taskDiscretMicroSeconds))
            {
                Console.WriteLine(MeanFailMessage);
                meanOk = false;
            }

            stdOk = true;
            if (standardDeviation >= taskDiscretMicroSeconds)
            {
                Console.WriteLine(StdFailMessage);
                stdOk = false;
            }

            Console.WriteLine();

            //выводим детальную статистику

            //for (var cc = 0; cc < points.Count; cc++)
            //{
            //    var d = points[cc];

            //    Console.WriteLine(
            //        "{0:D5} microsec  :  {1} - {2}",
            //        d.Diff,
            //        d.Left,
            //        d.Right
            //        );
            //}
        }

        private static List<Point> GenerateDiffs(
            int skipItemCount,
            long[] times,
            long jetMicroseconds,
            out long jetCount
            )
        {
            jetCount = 0;

            var points = new List<Point>();
            for (var cc = skipItemCount; cc < times.Length - 1; cc++)
            {
                var left = times[cc];
                var right = times[cc + 1];

                var diff = right - left;

                if (Math.Abs(diff) < jetMicroseconds)
                {
                    //это не считаем выбросом
                    points.Add(new Point(left, right));
                }
                else
                {
                    jetCount++;
                }
            }

            return
                points;
        }

        private static Scheduler.Scheduler CreateScheduler(
            IWaitGroupFactory waitGroupFactory
            )
        {
            if (waitGroupFactory == null)
            {
                throw new ArgumentNullException("waitGroupFactory");
            }

            var tf = new HighestPriorityThreadFactory(
                );

            var l = new TextSchedulerLogger(
                Console.WriteLine
                );

            var scheduler = new Scheduler.Scheduler(
                waitGroupFactory,
                tf,
                l
                );

            return
                scheduler;
        }

        #endregion
    }
}
