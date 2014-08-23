using Abot.Util;
using NUnit.Framework;
using System.Threading;

namespace Abot.Tests.Unit.Util
{
    [TestFixture]
    public class ManualThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new ManualThreadManager(maxThreads);
        }

        [Test]
        public void AbortAll_WorkCompletedAnyway()
        {
            ManualThreadManager uut = GetInstance(10) as ManualThreadManager;

            int count = 0;
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });
            uut.DoWork(() => { System.Threading.Thread.Sleep(50); Interlocked.Increment(ref count); });

            uut.AbortAll();//this does nothing to stop/cancel threads on this thread manager

            System.Threading.Thread.Sleep(250);
            Assert.AreEqual(5, count);
        }
    }
}
