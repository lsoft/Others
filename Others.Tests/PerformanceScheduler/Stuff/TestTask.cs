using System;
using Others.Helper;
using Others.Scheduler.Task;

namespace Others.Tests.PerformanceScheduler.Stuff
{
    internal class TestTask : BaseTask
    {
        private readonly int _repeatCount;
        private readonly long[] _times;
        private readonly PerformanceTimer _timer;

        private int _currentIteration = 0;

        public TestTask(
            int repeatCount,
            long microsecondsBetweenAwakes,
            long[] times
            )
            : base(Guid.NewGuid(), microsecondsBetweenAwakes)
        {
            if (times == null)
            {
                throw new ArgumentNullException("times");
            }

            _repeatCount = repeatCount;
            _times = times;
            _timer = new PerformanceTimer();
        }

        public override void Execute(out bool needToRepeat)
        {
            try
            {
                var current = _timer.MicroSeconds;
                _times[_currentIteration++] = current;
            }
            finally
            {
                needToRepeat = _currentIteration < _repeatCount;
            }
        }
    }
}