using System;
using System.Threading;

namespace Others.Disposer
{
    /// <summary>
    /// Optimistic disposer constructed with lock-free algorithm (into work method).
    /// It's faster that PessimisticDisposer in situations when work
    /// thread count is greather than logical CPU cores.
    /// Otherwise its performance is equivalent or slightly worse than PessimisticDisposer performance.
    /// </summary>
    public class OptimisticDisposer : IThreadSafeDisposer
    {
        private const long NoWorkersSignal = 0L;
        private const long ExitSignal = long.MinValue;

        private readonly object _disposeLocker = new object();

        private long _workers = NoWorkersSignal;

        public bool DoWorkSafely(
            Action action
            )
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            var beforeIterationValue = Interlocked.Read(ref _workers);

            if (beforeIterationValue < NoWorkersSignal)
            {
                //dispose marker has set

                return false;
            }

            var postIncrement = Interlocked.Increment(ref _workers);

            if (postIncrement < NoWorkersSignal)
            {
                //signal is in dispose interval
                //it means dispose is in progress
                //returns without actually work
                //it's okay
                //сигнал в зоне диспоуза
                //значит диспоуз вмешался
                //завершаемся без выполнения полезной работы (именно поэтому это не сломает диспоуз)

                //compensate a previous increment
                //вертаем взад тот признак что зря поставили
                Interlocked.Decrement(ref _workers);

                return false;
            }

            //workers signal has changed
            //no disposing detected
            //go ahead
            //воркеры заинкременчены, признаков совершающегося диспоуза не обнаружено
            //можно работать

            try
            {
                action();
            }
            finally
            {
                Interlocked.Decrement(ref _workers);
            }

            return true;
        }

        public void DoDisposeSafely(
            Action action
            )
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            //lock in dispose method is not a problem
            lock (_disposeLocker)
            {
                if (Interlocked.Read(ref _workers) < NoWorkersSignal)
                {
                    //dispose is in progress in another thread
                    //OR
                    //dispose already completed

                    return;
                }
                
                Interlocked.Add(ref _workers, long.MinValue);
            }

            //wait for an event that all workers has died
            while (Interlocked.Read(ref _workers) != ExitSignal)
            {
                //on or more workers are still alive
                //need to recheck after a while
                //for CPU ticks economy it's better to wait one time quant before next iteration of polling
                Thread.Yield();
            }

            //all workers has died
            //do dispose

            action();
        }
    }
}
