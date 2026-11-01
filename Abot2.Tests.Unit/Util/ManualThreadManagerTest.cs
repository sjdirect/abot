using System;
using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

//The thread managers are obsolete but still supported, so they keep their tests
#pragma warning disable CS0618

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class ManualThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new ObservableManualThreadManager(maxThreads);
        }

        [TestMethod]
        public void AbortAll_WorkCompletedAnyway()
        {
            var uut = (ObservableManualThreadManager)GetInstance(10);

            var count = 0;
            using (var finishWork = new ManualResetEventSlim())
            {
                for (var i = 0; i < 5; i++)
                {
                    uut.DoWork(() =>
                    {
                        finishWork.Wait(TestContext.CancellationToken);
                        Interlocked.Increment(ref count);
                    });
                }

                uut.AbortAll();//this does nothing to stop/cancel threads on this thread manager

                finishWork.Set();
                for (var i = 0; i < 5; i++)
                    uut.CompletedWork.Wait(TestContext.CancellationToken);
            }

            Assert.AreEqual(5, count);
        }

        private class ObservableManualThreadManager : ManualThreadManager, ICompletedWorkSignal
        {
            public ObservableManualThreadManager(int maxThreads)
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
