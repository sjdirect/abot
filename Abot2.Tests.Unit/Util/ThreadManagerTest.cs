using Abot2.Util;
using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//The thread managers are obsolete but still supported, so they keep their tests
#pragma warning disable CS0618

namespace Abot2.Tests.Unit.Util
{
    //Tests wait on signals instead of sleeping: work that must stay running waits on a gate the test opens,
    //and the thread manager under test reports each finished work item, so results never depend on timing.
    public abstract class ThreadManagerTest
    {
        IThreadManager _unitUnderTest;
        const int MAXTHREADS = 10;
        public TestContext TestContext { get; set; }

        protected abstract IThreadManager GetInstance(int maxThreads);

        [TestInitialize]
        public void SetUp()
        {
            _unitUnderTest = GetInstance(MAXTHREADS);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (_unitUnderTest != null)
                _unitUnderTest.Dispose();
        }

        [TestMethod]
        public void Constructor_CreatesDefaultInstance()
        {
            Assert.IsNotNull(_unitUnderTest);
            Assert.AreEqual(MAXTHREADS, _unitUnderTest.MaxThreads);
        }

        [TestMethod]
        public void Constructor_OverMax_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => GetInstance(101));
        }

        [TestMethod]
        public void Constructor_BelowMinimum()
        {
            Assert.ThrowsExactly<ArgumentException>(() => GetInstance(0));
        }

        [TestMethod]
        public void HasRunningThreads()
        {
            using (var workStarted = new ManualResetEventSlim())
            using (var finishWork = new ManualResetEventSlim())
            {
                //No threads should be running
                Assert.IsFalse(_unitUnderTest.HasRunningThreads());

                //Add work to be run on a thread
                _unitUnderTest.DoWork(() =>
                {
                    workStarted.Set();
                    finishWork.Wait(TestContext.CancellationToken);
                });
                workStarted.Wait(TestContext.CancellationToken);

                //Should have 1 running thread
                Assert.IsTrue(_unitUnderTest.HasRunningThreads());

                //Let the 1 running thread finish
                finishWork.Set();
                WaitForCompletedWork(1);

                //Should have 0 threads running since the thread has completed
                Assert.IsFalse(_unitUnderTest.HasRunningThreads());
            }
        }

        [TestMethod]
        public void DoWork_WorkItemsEqualToThreads_WorkIsCompletedAsync()
        {
            var count = 0;

            using (var finishWork = new ManualResetEventSlim())
            {
                for (var i = 0; i < MAXTHREADS; i++)
                {
                    _unitUnderTest.DoWork(() =>
                    {
                        finishWork.Wait(TestContext.CancellationToken);
                        Interlocked.Increment(ref count);
                    });
                }

                //DoWork returned while all the work is still waiting, so it runs asynchronously
                Assert.AreEqual(0, count);

                finishWork.Set();
                WaitForCompletedWork(MAXTHREADS);
                Assert.AreEqual(MAXTHREADS, count);
            }
        }

        [TestMethod]
        public void DoWork_MoreWorkThenThreads_WorkIsCompletedAsync()
        {
            var count = 0;
            for (var i = 0; i < 2 * MAXTHREADS; i++)
            {
                _unitUnderTest.DoWork(() => Interlocked.Increment(ref count));
            }

            WaitForCompletedWork(2 * MAXTHREADS);
            Assert.AreEqual(2 * MAXTHREADS, count);
        }

        [TestMethod]
        public void DoWork_SingleThreaded_WorkIsCompletedSynchronously()
        {
            _unitUnderTest = GetInstance(1);

            var count = 0;
            for (var i = 0; i < MAXTHREADS; i++)
            {
                _unitUnderTest.DoWork(() => Interlocked.Increment(ref count));
            }

            Assert.AreEqual(MAXTHREADS, count);
        }

        [TestMethod]
        public void DoWork_ActionIsNull_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _unitUnderTest.DoWork(null));
        }

        [TestMethod]
        public void DoWork_CalledAfterAbortAll_ThrowsException()
        {
            _unitUnderTest.AbortAll();

            Assert.ThrowsExactly<InvalidOperationException>(() => _unitUnderTest.DoWork(() => { }));
        }

        [TestMethod]
        public void Dispose()
        {
            Assert.IsTrue(_unitUnderTest is IDisposable);
        }

        [TestMethod]
        public void Abort_SetsHasRunningThreadsToZero()
        {
            using (var workStarted = new CountdownEvent(MAXTHREADS))
            using (var finishWork = new ManualResetEventSlim())
            {
                for (var i = 0; i < MAXTHREADS; i++)
                {
                    _unitUnderTest.DoWork(() =>
                    {
                        workStarted.Signal();
                        finishWork.Wait(TestContext.CancellationToken);
                    });
                }

                //Aborting cancels work that has not started yet, so make sure all of it is running
                workStarted.Wait(TestContext.CancellationToken);
                _unitUnderTest.AbortAll();
                Assert.IsFalse(_unitUnderTest.HasRunningThreads());

                finishWork.Set();
                WaitForCompletedWork(MAXTHREADS);
            }
        }

        [TestMethod]
        public void DoWork_CalledAfterDispose_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _unitUnderTest.DoWork(() => { }));
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _unitUnderTest.Dispose();
            _unitUnderTest.Dispose();
        }

        [TestMethod]
        public void Dispose_WhileThreadsAreRunning_DoesNotThrow()
        {
            using (var workStarted = new CountdownEvent(MAXTHREADS))
            using (var finishWork = new ManualResetEventSlim())
            {
                for (var i = 0; i < MAXTHREADS; i++)
                {
                    _unitUnderTest.DoWork(() =>
                    {
                        workStarted.Signal();
                        finishWork.Wait(TestContext.CancellationToken);
                    });
                }

                //Disposing cancels work that has not started yet, so make sure all of it is running
                workStarted.Wait(TestContext.CancellationToken);
                _unitUnderTest.Dispose();

                //Let the running threads finish against the disposed instance
                finishWork.Set();
                WaitForCompletedWork(MAXTHREADS);
            }
        }

        //Waits until the thread manager under test has finished the given number of work items, including its own bookkeeping
        protected void WaitForCompletedWork(int count)
        {
            var completedWork = ((ICompletedWorkSignal)_unitUnderTest).CompletedWork;
            for (var i = 0; i < count; i++)
                completedWork.Wait(TestContext.CancellationToken);
        }

        /// <summary>
        /// Implemented by the thread managers under test: released once for every work item they finish
        /// </summary>
        protected interface ICompletedWorkSignal
        {
            SemaphoreSlim CompletedWork { get; }
        }
    }
}
