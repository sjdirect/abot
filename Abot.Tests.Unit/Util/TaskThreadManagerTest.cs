using Abot.Util;
using NUnit.Framework;
using System.Threading;

namespace Abot.Tests.Unit.Util
{
    [TestFixture]
    public class TaskThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new TaskThreadManager(maxThreads);
        }

        [Test]
        public void AbortAll_WorkNeverCompleted()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();   
            TaskThreadManager uut = new TaskThreadManager(10, cancellationTokenSource);

            int count = 0;
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });

            uut.AbortAll();

            System.Threading.Thread.Sleep(500);
            Assert.IsTrue(count < 5);
        }

        [Test]
        public void DoWork_TokenCanceled_WorkNeverCompleted()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            TaskThreadManager uut = new TaskThreadManager(10, cancellationTokenSource);

            int count = 0;
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
