using Abot.Core;
using NUnit.Framework;
using System;
using System.Threading;

namespace Abot.Tests.Unit.Core
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
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_OverMax()
        {
            _unitUnderTest = GetInstance(101);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_BelowMinimum()
        {
            _unitUnderTest = GetInstance(0);
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
            System.Threading.Thread.Sleep(20);
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
            System.Threading.Thread.Sleep(20);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void DoWork_ActionIsNull()
        {
            _unitUnderTest.DoWork(null);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DoWork_CalledAfterAbortAll()
        {
            _unitUnderTest.AbortAll();
            _unitUnderTest.DoWork(() => System.Threading.Thread.Sleep(10));
        }

        [Test]
        public void AbortAll_WorkNeverCompleted()
        {
            int count = 0;

            _unitUnderTest.DoWork(() => { System.Threading.Thread.Sleep(1000); count++; });
            _unitUnderTest.DoWork(() => { System.Threading.Thread.Sleep(1000); count++; });
            _unitUnderTest.DoWork(() => { System.Threading.Thread.Sleep(1000); count++; });
            _unitUnderTest.DoWork(() => { System.Threading.Thread.Sleep(1000); count++; });
            _unitUnderTest.DoWork(() => { System.Threading.Thread.Sleep(1000); count++; });

            _unitUnderTest.AbortAll();

            System.Threading.Thread.Sleep(250);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Dispose()
        {
            Assert.IsTrue(_unitUnderTest is IDisposable);
        }
    }
}
