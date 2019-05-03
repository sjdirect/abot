using Abot2.Util;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class CachedMemoryMonitorTest
    {
        CachedMemoryMonitor _uut;
        Mock<IMemoryMonitor> _fakeMemoryMonitor;

        [TestInitialize]
        public void Setup()
        {
            _fakeMemoryMonitor = new Mock<IMemoryMonitor>();
        }

        [TestCleanup]
        public void TearDown()
        {
            if(_uut != null)
                _uut.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_MemoryMonitorIsNull_ThrowsException()
        {
            new CachedMemoryMonitor(null, 1);
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsZero_CallsMemoryMonitor()
        {
            new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 0);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsNegative_CallsMemoryMonitor()
        {
            new CachedMemoryMonitor(_fakeMemoryMonitor.Object, -1);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }

        [TestMethod]
        public void Constructor_CacheExpirationIsValid_CallsMemoryMonitorEveryCacheExpirationInterval()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 1);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));

            System.Threading.Thread.Sleep(1200);

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(2));
        }

        [TestMethod]
        public void GetCurrentUsageInMb_SecondCall_DoesNotCallsMemoryMonitorSinceValueWasCached()
        {
            _uut = new CachedMemoryMonitor(_fakeMemoryMonitor.Object, 1);

            _uut.GetCurrentUsageInMb();

            _fakeMemoryMonitor.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(1));
        }
    }
}
