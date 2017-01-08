using Abot.Util;
using Moq;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Util
{
    [TestFixture]
    public class MemoryManagerTest
    {
        MemoryManager _uut;
        Mock<IMemoryMonitor> _fakeMemoryManager;

        [SetUp]
        public void SetUp()
        {
            _fakeMemoryManager = new Mock<IMemoryMonitor>();

            _uut = new MemoryManager(_fakeMemoryManager.Object);
        }

        [TearDown]
        public void TearDownAttribute()
        {
            if(_uut != null)
                _uut.Dispose();
        }

        [Test]
        public void Constructor_MemoryMonitorIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MemoryManager(null));
        }


        [Test]
        public void IsCurrentUsageAbove_IsAbove_ReturnsTrue()
        {
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsTrue(_uut.IsCurrentUsageAbove(99));
        }

        [Test]
        public void IsCurrentUsageAbove_EqualTo_ReturnsFalse()
        {
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsFalse(_uut.IsCurrentUsageAbove(100));
        }

        [Test]
        public void IsCurrentUsageAbove_IsBelow_ReturnsFalse()
        {
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(100);

            Assert.IsFalse(_uut.IsCurrentUsageAbove(101));
        }


        [Test]
        public void IsSpaceAvailable_Zero_ReturnsTrue()
        {
            Assert.IsTrue(_uut.IsSpaceAvailable(0));
        }

        [Test]
        public void IsSpaceAvailable_Negative_ReturnsTrue()
        {
            Assert.IsTrue(_uut.IsSpaceAvailable(-1));
        }

        [Test]
        public void IsSpaceAvailable_Available_ReturnsTrue()
        {
            Assert.IsTrue(_uut.IsSpaceAvailable(1));
        }

        [Test]
        public void IsSpaceAvailable_NotAvailable_ReturnsFalse()
        {
            Assert.IsFalse(_uut.IsSpaceAvailable(9999999));
        }
    }
}
