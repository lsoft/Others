using System;
using System.Threading;

namespace Others.Disposer
{
    /// <summary>
    /// Pessimistic (locking) disposer.
    /// Пессимистичный диспоузер, построенный на блокировке.
    /// </summary>
    public class PessimisticDisposer : IThreadSafeDisposer
    {
        private readonly object _locker = new object();

        private volatile bool _disposed = false;

        private long _workers = 0L;

        public bool DoWorkSafely(
            Action action
            )
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            //do detection of disposing
            lock (_locker)
            {
                if (_disposed)
                {
                    return false;
                }

                Interlocked.Increment(ref _workers);
            }

            //no dispose detected
            try
            {
                //actually do work
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

            //wait for all workers has finished
            lock (_locker)
            {
                //if it's already disposed then return
                if (_disposed)
                {
                    return;
                }

                //set dispose marker
                _disposed = true;

                //wait for all RUNNING workers has finished
                while (Interlocked.Read(ref _workers) != 0L)
                {
                    Thread.Yield();
                }
            }

            //actually do dispose
            action();
        }
    }
}