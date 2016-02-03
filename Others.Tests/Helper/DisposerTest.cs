using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.Disposer;

namespace Others.Tests.Helper
{
    public class DisposerTest
    {
        private readonly IThreadSafeDisposer _disposer;
        private readonly Action<int> _workAction;
        private readonly Action _disposeAction;

        private ManualResetEventSlim _startEvent;
        private ManualResetEventSlim _stopEvent;

        private bool _testFinished = false;

        public DisposerTest(
            IThreadSafeDisposer disposer,
            Action<int> workAction,
            Action disposeAction
            )
        {
            if (disposer == null)
            {
                throw new ArgumentNullException("disposer");
            }
            if (workAction == null)
            {
                throw new ArgumentNullException("workAction");
            }
            if (disposeAction == null)
            {
                throw new ArgumentNullException("disposeAction");
            }

            _disposer = disposer;
            _workAction = workAction;
            _disposeAction = disposeAction;
        }

        public void DoTest(
            int threadCount,
            int testTimeout
            )
        {
            if (_testFinished)
            {
                throw new InvalidOperationException("testFinished");
            }

            _testFinished = true;


            _startEvent = new ManualResetEventSlim(false);
            _stopEvent = new ManualResetEventSlim(false);

            using (_startEvent)
            {
                using (_stopEvent)
                {
                    var tl = new List<Thread>();
                    for (var cc = 0; cc < threadCount; cc++)
                    {
                        var t = new Thread(WorkThread);
                        tl.Add(t);
                    }

                    for (var ti = 0; ti < tl.Count; ti++)
                    {
                        tl[ti].Start(ti);
                    }

                    _startEvent.Set();

                    Thread.Sleep(testTimeout);

                    var disposeThread = new Thread(DisposeThread);
                    disposeThread.Start();
                    if (!disposeThread.Join(10000))
                    {
                        //слишком долго ждали диспоуза
                        //это "дедлок", и ошибка
                        throw new InternalTestFailureException("слишком долго ждали диспоуза, это дедлок, и ошибка");
                    }


                    _stopEvent.Set();

                    tl.ForEach(j => j.Join());
                }
            }
        }

        private void DisposeThread()
        {
            _disposer.DoDisposeSafely(_disposeAction);
        }

        private void WorkThread(object arg)
        {
            var intArg = (int) arg;

            while (!_startEvent.Wait(0))
            {
                //ждем разрешения стартануть
            }

            //работаем
            while (!_stopEvent.Wait(0))
            {
                //побольше напихаем и увеличим конкуррентность
                for (var i = 0; i < 100; i++)
                {
                    _disposer.DoWorkSafely(() => _workAction(intArg));
                }
            }
        }
    }
}