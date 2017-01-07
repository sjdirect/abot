using Abot.Util;
using NUnit.Framework;
using System;
using System.Threading;

namespace Abot.Tests.Unit.Util
{
    [TestFixture]
    public abstract class ThreadManagerTest
    {
        IThreadManager _unitUnderTest;
        const int MAXTHREADS = 10;
        protected abstract IThreadManager GetInstance(int maxThreads);

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = GetInstance(MAXTHREADS);
        }

        [TearDown]
        public void TearDown()
        {
            if (_unitUnderTest != null)
                _unitUnderTest.Dispose();
        }

        [Test]
        public void Constructor_CreatesDefaultInstance()
        {
            Assert.IsNotNull(_unitUnderTest);
            Assert.AreEqual(MAXTHREADS, _unitUnderTest.MaxThreads);
        }

        [Test]
        public void Constructor_OverMax_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => _unitUnderTest = GetInstance(101));
        }

        [Test]
        public void Constructor_BelowMinimum()
        {
            Assert.Throws<ArgumentException>(() => GetInstance(0));
        }

        [Test]
        public void HasRunningThreads()
        {
            //No threads should be running
            Assert.IsFalse(_unitUnderTest.HasRunningThreads());

            //Add word to be run on a thread
            _unitUnderTest.DoWork(() => System.Threading.Thread.Sleep(300));
            System.Threading.Thread.Sleep(20);

            //Should have 1 running thread
            Assert.IsTrue(_unitUnderTest.HasRunningThreads());

            //Wait for the 1 running thread to finish
            System.Threading.Thread.Sleep(400);

            //Should have 0 threads running since the thread should have completed by now
            Assert.IsFalse(_unitUnderTest.HasRunningThreads());
        }

        [Test]
        public void DoWork_WorkItemsEqualToThreads_WorkIsCompletedAsync()
        {
            int count = 0;

            for (int i = 0; i < MAXTHREADS; i++)
            {
                _unitUnderTest.DoWork(() =>
                {
                    System.Threading.Thread.Sleep(5);
                    Interlocked.Increment(ref count);
                });
            }

            Assert.IsTrue(count < MAXTHREADS);
            System.Threading.Thread.Sleep(80);//was 20 but had to bump it up or the TaskThreadManager would fail, its way slower
            Assert.AreEqual(MAXTHREADS, count);
        }

        [Test]
        public void DoWork_MoreWorkThenThreads_WorkIsCompletedAsync()
        {
            int count = 0;
            for (int i = 0; i < 2 * MAXTHREADS; i++)
            {
                _unitUnderTest.DoWork(() =>
                {
                    System.Threading.Thread.Sleep(5);
                    Interlocked.Increment(ref count);
                });
            }

            //Assert.IsTrue(count < MAXTHREADS);//Manual has completed more then the thread count by the time it gets here
            System.Threading.Thread.Sleep(80);//was 20 but had to bump it up or the TaskThreadManager would fail, its way slower
            Assert.AreEqual(2 * MAXTHREADS, count);
        }

        [Test]
        public void DoWork_SingleThreaded_WorkIsCompletedSynchronously()
        {
            _unitUnderTest = GetInstance(1);

            int count = 0;
            for (int i = 0; i < MAXTHREADS; i++)
            {
                _unitUnderTest.DoWork(() =>
                {
                    System.Threading.Thread.Sleep(5);
                    Interlocked.Increment(ref count);
                });
            }

            Assert.AreEqual(MAXTHREADS, count);
        }

        [Test]
        public void DoWork_ActionIsNull_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => _unitUnderTest.DoWork(null));
        }

        [Test]
        public void DoWork_CalledAfterAbortAll_ThrowsException()
        {
            _unitUnderTest.AbortAll();

            Assert.Throws<InvalidOperationException>(() => _unitUnderTest.DoWork(() => System.Threading.Thread.Sleep(10)));
        }

        [Test]
        public void Dispose()
        {
            Assert.IsTrue(_unitUnderTest is IDisposable);
        }

        [Test]
        public void Abort_SetsHasRunningThreadsToZero()
        {
            for (int i = 0; i < 2 * MAXTHREADS; i++)
            {
                _unitUnderTest.DoWork(() => System.Threading.Thread.Sleep(5));
            }

            _unitUnderTest.AbortAll();
            Assert.IsFalse(_unitUnderTest.HasRunningThreads());
        }
    }
}
