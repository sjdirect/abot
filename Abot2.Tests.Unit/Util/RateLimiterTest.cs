using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

//RateLimiter is obsolete but still supported, so it keeps its tests
#pragma warning disable CS0618

namespace Abot2.Tests.Unit.Util
{
    //Waiting is tested on a fake clock that only moves when a test advances it, so results never depend on machine load.
    //WaitToProceed(0) answers "may it proceed now?" without blocking.
    [TestClass]
    public class RateLimiterTest
    {
        RateLimiter _unitUnderTest;

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Constructor_InvalidOccurrances()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RateLimiter(0, TimeSpan.FromSeconds(10)));
        }

        [TestMethod]
        public void Constructor_NegativeTimeSpan()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RateLimiter(1, TimeSpan.FromSeconds(-10)));
        }

        [TestMethod]
        public void Constructor_TimeSpanToBig()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new RateLimiter(1, TimeSpan.FromDays(1000)));
        }

        [TestMethod]
        public void WaitToProceed_ZeroWait_DoesntWait()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(0), new ManualTimeProvider());

            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
        }

        [TestMethod]
        public void WaitToProceed_ValidWaitTime_Waits()
        {
            var time = new ManualTimeProvider();
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromMilliseconds(700), time);

            Assert.IsTrue(_unitUnderTest.WaitToProceed(0), "The first call should not wait");
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0), "The second call should wait");

            time.Advance(TimeSpan.FromMilliseconds(699));
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0), "The time unit has not passed yet");

            time.Advance(TimeSpan.FromMilliseconds(1));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0), "The time unit has passed");
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0), "The next call should wait a full time unit again");

            time.Advance(TimeSpan.FromMilliseconds(700));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
        }

        [TestMethod]
        public void WaitToProceed_FirstCallLaterThanCreation_WaitsAFullTimeUnitFromThatCall()
        {
            var time = new ManualTimeProvider();
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromMilliseconds(700), time);

            //Called partway through the limiter's own timer cycle, so the wait must be measured from this call
            time.Advance(TimeSpan.FromMilliseconds(300));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0));

            time.Advance(TimeSpan.FromMilliseconds(699));
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0), "700 ms have not passed since the first call");

            time.Advance(TimeSpan.FromMilliseconds(1));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0), "700 ms have passed since the first call");
        }

        [TestMethod]
        public void WaitToProceed_MultipleOccurrences_AllowsThatManyPerTimeUnit()
        {
            var time = new ManualTimeProvider();
            _unitUnderTest = new RateLimiter(2, TimeSpan.FromMilliseconds(100), time);

            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0), "Only 2 calls are allowed per time unit");

            time.Advance(TimeSpan.FromMilliseconds(100));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsTrue(_unitUnderTest.WaitToProceed(0));
            Assert.IsFalse(_unitUnderTest.WaitToProceed(0));
        }

        [TestMethod]
        public void WaitToProceed_ForCoverage()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1), new ManualTimeProvider());

            Assert.IsTrue(_unitUnderTest.WaitToProceed(TimeSpan.FromSeconds(10)), "A free slot should be taken without waiting");
            _unitUnderTest.Dispose();
        }

        [TestMethod]
        public void WaitToProceed_NegativeMilli()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _unitUnderTest.WaitToProceed(-10));
        }

        [TestMethod]
        public void Dispose_ThenWaitToProceed_ThrowsObjectDisposedException()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1));

            _unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _unitUnderTest.WaitToProceed());
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var time = new ManualTimeProvider();
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromMilliseconds(10), time);
            _unitUnderTest.WaitToProceed();

            _unitUnderTest.Dispose();
            _unitUnderTest.Dispose();

            //Past the time a timer started before the dispose was due, so any such timer still scheduled fires against the disposed instance
            time.Advance(TimeSpan.FromMilliseconds(10));
        }
    }
}
