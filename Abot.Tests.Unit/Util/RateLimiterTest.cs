using Abot.Util;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Abot.Tests.Unit.Util
{
    [TestFixture]
    public class RateLimiterTest
    {
        RateLimiter _unitUnderTest;

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_InvalidOccurrances()
        {
            new RateLimiter(0, TimeSpan.FromSeconds(10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_NegativeTimeSpan()
        {
            new RateLimiter(1, TimeSpan.FromSeconds(-10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_TimeSpanToBig()
        {
            new RateLimiter(1, TimeSpan.FromDays(1000));
        }

        [Test]
        public void WaitToProceed_ZeroWait_DoesntWait()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(0));

            Stopwatch timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds < 1);
        }

        [Test]
        public void WaitToProceed_OneSecWait_Waits()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(2));

            Stopwatch timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed();//will not wait for first call
            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds > 2);
        }

        [Test]
        public void WaitToProceed_PartialSecWait_Waits()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1.5));

            Stopwatch timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed();//will not wait for first call
            _unitUnderTest.WaitToProceed();
            _unitUnderTest.WaitToProceed();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds > 1.5);
        }

        [Test]
        public void WaitToProceed_ForCoverage()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1));

            Stopwatch timer = Stopwatch.StartNew();

            _unitUnderTest.WaitToProceed(TimeSpan.FromSeconds(10));
            _unitUnderTest.Dispose();

            timer.Stop();

            Assert.IsTrue(timer.Elapsed.TotalSeconds < 1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WaitToProceed_NegativeMilli()
        {
            _unitUnderTest = new RateLimiter(1, TimeSpan.FromSeconds(1));
            _unitUnderTest.WaitToProceed(-10);
        }
    }
}
