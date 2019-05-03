using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class TaskThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new TaskThreadManager(maxThreads);
        }

        [TestMethod]
        public void AbortAll_WorkNeverCompleted()
        {
            var cancellationTokenSource = new CancellationTokenSource();   
            var uut = new TaskThreadManager(10, cancellationTokenSource);

            var count = 0;
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });

            //cancellationTokenSource.Cancel();
            uut.AbortAll();

            System.Threading.Thread.Sleep(500);
            Assert.IsTrue(count < 5);
        }

        [TestMethod]
        public void DoWork_TokenCanceled_WorkNeverCompleted()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var uut = new TaskThreadManager(10, cancellationTokenSource);

            var count = 0;
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });

            cancellationTokenSource.Cancel();

            System.Threading.Thread.Sleep(250);
            Assert.IsTrue(count < 5);
        }
    }
}
