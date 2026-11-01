using Abot2.Util;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Util
{
    //The refresh timer runs on a manual clock that only moves when a test advances it, so results never depend on timing
    [TestClass]
    public class CachedMemoryMonitorTest
    {
        CachedMemoryMonitor _uut;
        Mock<IMemoryMonitor> _fakeMemoryMonitor;
        ManualTimeProvider _time;

        [TestInitialize]
        public void Setup()
        {
            _fakeMemoryMonitor = new Mock<IMemoryMonitor>();
            _time = new ManualTimeProvider();
        }

        [TestCleanup]
        public void TearDown()
        {
            if(_uut != null)
                _uut.Dispose();
        }

        [TestMethod]
        public void Constructor_MemoryMonitorIsNull_ThrowsException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new CachedMemoryMonitor(null, 1, _time));
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsZero_CallsMemoryMonitor()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 0, _time);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsNegative_CallsMemoryMonitor()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, -1, _time);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsValid_CallsMemoryMonitorEveryCacheExpirationInterval()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 1, _time);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));

            _time.Advance(TimeSpan.FromMilliseconds(999));
            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));

            _time.Advance(TimeSpan.FromMilliseconds(1));
            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(2));

            _time.Advance(TimeSpan.FromSeconds(1));
            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(3));
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsZero_RefreshesEveryFiveSeconds()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 0, _time);

            _time.Advance(TimeSpan.FromMilliseconds(4999));
            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));

            _time.Advance(TimeSpan.FromMilliseconds(1));
            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(2));
        }

        [TestMethod]
        public void GetCurrentUsageInMb_SecondCall_DoesNotCallsMemoryMonitorSinceValueWasCached()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 1, _time);

            _uut.GetCurrentUsageInMb();

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }

        [TestMethod]
        public void Dispose_StopsRefreshing()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 1, _time);

            _uut.Dispose();
            _time.Advance(TimeSpan.FromSeconds(5));

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }

        [TestMethod]
        public void Dispose_DoesNotDisposeMemoryMonitorPassedIn()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 1, _time);

            _uut.Dispose();
            _uut.Dispose();

            _fakeMemoryMonitor.Verify(f => f.Dispose(), Times.Never);
        }
    }
}
