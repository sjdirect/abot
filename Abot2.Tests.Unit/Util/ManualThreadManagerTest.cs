using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class ManualThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new ManualThreadManager(maxThreads);
        }

        [TestMethod]
        public void AbortAll_WorkCompletedAnyway()
        {
            var uut = GetInstance(10) as ManualThreadManager;

            var count = 0;
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
