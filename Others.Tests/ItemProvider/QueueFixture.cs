using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Others.ItemProvider;
using Others.Tests.ItemProvider.Stuff;

namespace Others.Tests.ItemProvider
{
    internal class QueueFixture
    {
        private readonly IItemWaitProvider<Item> _itemWaitProvider;

        private readonly ManualResetEvent _threadWorkEvent = new ManualResetEvent(false);

        private Item[][] _datas;
        private long _accumulator = 0L;

        private long _totalCount = 0L;
        private long _writeCount = 0L;
        private long _readCount = 0L;

        private List<Thread> _allThreads;

        private string _forceAbortReason;

        public QueueFixture(
            IItemWaitProvider<Item> itemWaitProvider
            )
        {
            if (itemWaitProvider == null)
            {
                throw new ArgumentNullException("itemWaitProvider");
            }

            _itemWaitProvider = itemWaitProvider;
        }


        public void AggregationTest()
        {
            var random = new Random(
                int.Parse(Guid.NewGuid().ToString().Replace("-", "").Substring(0, 7), NumberStyles.HexNumber)
                );

            //��������� �����
            var writeThreadCount = 10;// Environment.ProcessorCount + 2;
            var readThreadCount = 6;//Environment.ProcessorCount;// * 2;
            var itemCount = 100000;
            var maxValue = 1000;

            //���������� ������
            var correctAccumulator = 0L;

            _datas = new Item[writeThreadCount][];
            for (var di = 0; di < writeThreadCount; di++)
            {
                _datas[di] = new Item[itemCount];

                for (var ii = 0; ii < itemCount; ii++)
                {
                    var v = random.Next(maxValue) + 1;

                    _datas[di][ii] = new Item(v);

                    //Debug.WriteLine("Generated {0}", v);

                    correctAccumulator += v;

                    _totalCount++;
                }
            }

            Debug.WriteLine("CorrectAccumulator {0}", correctAccumulator);

            var writeThreads = new Thread[writeThreadCount];
            for (var ti = 0; ti < writeThreads.Length; ti++)
            {
                var wt = new Thread(WriteThread);
                //wt.Priority = ThreadPriority.AboveNormal;

                writeThreads[ti] = wt;
            }

            var readThreads = new Thread[readThreadCount];
            for (var ti = 0; ti < readThreads.Length; ti++)
            {
                readThreads[ti] = new Thread(ReadThread);
            }

            _allThreads = writeThreads.ToList();
            _allThreads.AddRange(readThreads);

            //run the test
            for (var ti = 0; ti < writeThreads.Length; ti++)
            {
                writeThreads[ti].Start(ti);
            }
            for (var ti = 0; ti < readThreads.Length; ti++)
            {
                readThreads[ti].Start(ti);
            }

            _threadWorkEvent.Set();

            var before = DateTime.Now;

            //��� ��������!

            for (var ti = 0; ti < _allThreads.Count; ti++)
            {
                _allThreads[ti].Join();
            }

            var after = DateTime.Now;

            Item r;
            if (_itemWaitProvider.GetItem(TimeSpan.Zero, out r) == OperationResultEnum.Success)
            {
                Assert.Fail("���� ����������!");
            }

            ((IDisposable)_itemWaitProvider).Dispose();

            Debug.WriteLine("Total count {0}, written count {1}, read count {2}", _totalCount, _writeCount, _readCount);
            Debug.WriteLine("Expected {0}, taken {1}", correctAccumulator, _accumulator);
            Debug.WriteLine("Time taken {0}", after - before);

            if (string.IsNullOrEmpty(_forceAbortReason))
            {
                Assert.AreEqual(correctAccumulator, _accumulator);
            }
            else
            {
                Debug.WriteLine("FORCE ABORT: " + _forceAbortReason);

                Assert.Fail();

            }
        }

        private void ForceAbort(string reason)
        {
            _forceAbortReason = reason;

            _allThreads.Where(j => j.ManagedThreadId != Thread.CurrentThread.ManagedThreadId).ToList().ForEach(j => j.Abort());

            Thread.CurrentThread.Abort();
        }

        private void WriteThread(object arg)
        {
            //���� ������� � ������ ������
            _threadWorkEvent.WaitOne();

            var threadIndex = (int)arg;

            //���� ����������
            var q = _datas[threadIndex];

            for (var ii = 0; ii < q.Length; ii++)
            {
                var result = _itemWaitProvider.AddItem(q[ii]);

                //if(threadIndex == 0)
                if (result != OperationResultEnum.Success)
                {
                    ForceAbort("������ ���������� �����");
                }

                //Debug.WriteLine("Stored {0}", q[ii].Value);

                Interlocked.Increment(ref _writeCount);
            }
        }

        private void ReadThread(object arg)
        {
            //���� ������� � ������ ������
            _threadWorkEvent.WaitOne();

            var threadIndex = (int)arg;

            //���� ����������
            var localAccumulator = 0L;

            while (Interlocked.Read(ref _readCount) < Interlocked.Read(ref _totalCount))
                //while (true)
            {
                Item item;
                var result = _itemWaitProvider.GetItem(
                    TimeSpan.FromMilliseconds(150),
                    out item
                    );

                switch (result)
                {
                    case OperationResultEnum.Success:
                        localAccumulator += item.Value;
                        //Debug.WriteLine("Taken {0}", item.Value);
                        Interlocked.Increment(ref _readCount);
                        break;
                    case OperationResultEnum.Timeout:
                        //goto exit;
                        break;
                    case OperationResultEnum.Dispose:
                        ForceAbort("Read thread: result is OperationResultEnum.Dispose");
                        return;
                    default:
                        ForceAbort("Unknown value of result: " + result.ToString());
                        return;
                }
            }

            exit:
            Interlocked.Add(ref _accumulator, localAccumulator);
        }
    }
}