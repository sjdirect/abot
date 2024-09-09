using Abot2.Util;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Util
{
    [TestClass]
    public class MemoryManagerTest
    {
        MemoryManager _uut;
        Mock<IMemoryMonitor> _fakeMemoryMonitor;

        [TestInitialize]
        public void SetUp()
        {
            _fakeMemoryMonitor = new Mock<IMemoryMonitor>();

            _uut = new MemoryManager(_fakeMemoryMonitor.Object);
        }

        [TestCleanup]
        public void TearDownAttribute()
        {
            if(_uut != null)
                _uut.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_MemoryMonitorIsNull()
        {
            new MemoryManager(null);
        }


        [TestMethod]
        public void IsCurrentUsageAbove_IsAbove_ReturnsTrue()
        {
            _fakeMemoryMonitor.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsTrue(_uut.IsCurrentUsageAbove(99));
        }

        [TestMethod]
        public void IsCurrentUsageAbove_EqualTo_ReturnsFalse()
        {
            _fakeMemoryMonitor.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsFalse(_uut.IsCurrentUsageAbove(100));
        }

        [TestMethod]
        public void IsCurrentUsageAbove_IsBelow_ReturnsFalse()
        {
            _fakeMemoryMonitor.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsFalse(_uut.IsCurrentUsageAbove(101));
        }


        [TestMethod]
        public void IsSpaceAvailable_Zero_ReturnsTrue()
        {
            Assert.IsTrue(_uut.IsSpaceAvailable(0));
        }

        [TestMethod]
        public void IsSpaceAvailable_Negative_ReturnsTrue()
        {
            Assert.IsTrue(_uut.IsSpaceAvailable(-1));
        }

        [TestMethod]
        public void IsSpaceAvailable_Available_ReturnsTrue()
        {
            Assert.IsTrue(_uut.IsSpaceAvailable(1));
        }

        [TestMethod, Ignore("This test currently fails as the InsufficientMemoryException never gets thrown so it always looks like space is available. Likely an issue with unix/linux implementation of MemoryFailPoint.")]
        public void IsSpaceAvailable_NotAvailable_ReturnsFalse()
        {
            Assert.IsFalse(_uut.IsSpaceAvailable(Int32.MaxValue), "This much memory is not available on any system");
        }
    }
}
