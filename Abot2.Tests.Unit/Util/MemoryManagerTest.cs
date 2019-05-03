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
        Mock<IMemoryMonitor> _fakeMemoryManager;

        [TestInitialize]
        public void SetUp()
        {
            _fakeMemoryManager = new Mock<IMemoryMonitor>();

            _uut = new MemoryManager(_fakeMemoryManager.Object);
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
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsTrue(_uut.IsCurrentUsageAbove(99));
        }

        [TestMethod]
        public void IsCurrentUsageAbove_EqualTo_ReturnsFalse()
        {
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsFalse(_uut.IsCurrentUsageAbove(100));
        }

        [TestMethod]
        public void IsCurrentUsageAbove_IsBelow_ReturnsFalse()
        {
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

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

        [TestMethod]
        public void IsSpaceAvailable_NotAvailable_ReturnsFalse()
        {
            Assert.IsFalse(_uut.IsSpaceAvailable(9999999));
        }
    }
}
