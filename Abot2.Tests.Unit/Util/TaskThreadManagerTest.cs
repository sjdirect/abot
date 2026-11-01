using System;
using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

//The thread managers are obsolete but still supported, so they keep their tests
#pragma warning disable CS0618

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class TaskThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new ObservableTaskThreadManager(maxThreads);
        }

        //Cancellation only stops work that has not started yet, work already running is not interrupted. These tests used to cancel
        //right after queuing work and assert some of it never ran, which raced the thread pool and failed whenever the test thread
        //was delayed for a millisecond before cancelling. They now only assert what cancellation actually guarantees.

        [TestMethod]
        public void AbortAll_CancelsTokenSource()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var uut = new TaskThreadManager(10, cancellationTokenSource);

            uut.AbortAll();

            Assert.IsTrue(cancellationTokenSource.IsCancellationRequested);
        }

        [TestMethod]
        public void DoWork_TokenCanceled_WorkNeverStarts()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var uut = new TaskThreadManager(10, cancellationTokenSource);
            cancellationTokenSource.Cancel();

            //A task started with an already cancelled token is cancelled before DoWork returns, so it can never run afterwards
            var count = 0;
            for (var i = 0; i < 5; i++)
                uut.DoWork(() => Interlocked.Increment(ref count));

            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void Dispose_CancelsButDoesNotDisposeTokenSourcePassedIn()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var uut = new TaskThreadManager(10, cancellationTokenSource);

            uut.Dispose();

            Assert.IsTrue(cancellationTokenSource.IsCancellationRequested);
            _ = cancellationTokenSource.Token;//throws ObjectDisposedException if it was disposed
            cancellationTokenSource.Dispose();
        }

        [TestMethod]
        public void AbortAll_AfterTokenSourcePassedInWasDisposed_DoesNotThrow()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var uut = new TaskThreadManager(10, cancellationTokenSource);
            cancellationTokenSource.Dispose();

            uut.AbortAll();
            uut.Dispose();
        }

        private class ObservableTaskThreadManager : TaskThreadManager, ICompletedWorkSignal
        {
            public ObservableTaskThreadManager(int maxThreads)
                : base(maxThreads)
            {
            }

            //Not disposed: a work item finishing after the test may still release it, and SemaphoreSlim holds no unmanaged resources unless its wait handle is used
            public SemaphoreSlim CompletedWork { get; } = new SemaphoreSlim(0);

            protected override void RunAction(Action action, bool decrementRunningThreadCountOnCompletion = true)
            {
                try
                {
                    base.RunAction(action, decrementRunningThreadCountOnCompletion);
                }
                finally
                {
                    CompletedWork.Release();
                }
            }
        }
    }
}
