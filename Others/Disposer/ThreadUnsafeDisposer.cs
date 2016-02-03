using System;

namespace Others.Disposer
{
    /// <summary>
    /// Fake unsafe disposer. It's useful as a baseline in performance tests.
    /// ������������ ��������. ������������ � ������ ������������������ ��� �������.
    /// </summary>
    public class ThreadUnsafeDisposer : IThreadSafeDisposer
    {
        public bool DoWorkSafely(Action action)
        {
            action();

            return true;
        }

        public void DoDisposeSafely(Action action)
        {
            action();
        }
    }
}