using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class RateLimiterTest
    {
        RateLimiter _unitUnderTest;

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_InvalidOccurrances()
        {
            new RateLimiter(0, TimeSpan.FromSeconds(10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_NegativeTimeSpan()
        {
            new RateLimiter(1, TimeSpan.FromSeconds(-10));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_TimeSpanToBig()
        {
            new RateLimiter(1, TimeSpan.FromDays(1000));
        }

        [TestMethod]
        public void WaitToProceed_ZeroWait_DoesntWait()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(0));

            var timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds < 1);
        }

        [TestMethod]
        public void WaitToProceed_ValidWaitTime_Waits()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromMilliseconds(700));

            var timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed();//will not wait for first call
            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds > 1);
        }

        [TestMethod]
        public void WaitToProceed_ForCoverage()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1));

            var timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed(TimeSpan.FromSeconds(10));
            _unitUnderTest.Dispose();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds < 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WaitToProceed_NegativeMilli()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1));
            _unitUnderTest.WaitToProceed(-10);
        }
    }
}
