using System;

namespace Others.Disposer
{
    /// <summary>
    /// A component that guard dispose method to be executed in exclusive environment.
    /// </summary>
    public interface IThreadSafeDisposer
    {
        /// <summary>
        /// Do work. All work calls are performing in parallel.
        /// </summary>
        /// <param name="action">Work action.</param>
        /// <returns>true - work completed successfully, false - work cancelled due to performing dispose.</returns>
        bool DoWorkSafely(
            Action action
            );

        /// <summary>
        /// Do dispose. Dispose call is performing in an exclusive manner, after all work calls has finished or cancelled.
        /// </summary>
        /// <param name="action">Dispose action.</param>
        void DoDisposeSafely(
            Action action
            );
    }
}