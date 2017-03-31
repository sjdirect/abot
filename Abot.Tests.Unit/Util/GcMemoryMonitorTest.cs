using Abot.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Abot.Tests.Unit.Util
{
    [TestFixture]
    public class GcMemoryMonitorTest
    {
        GcMemoryMonitor _uut;

        [SetUp]
        public void Setup()
        {
            _uut = new GcMemoryMonitor();
        }

        [TearDown]
        public void TearDown()
        {
            if(_uut != null)
                _uut.Dispose();
        }

        [Test]
        public void GetCurrentUsageInMb_ReturnsAValueAboveZero()
        {
            Assert.IsTrue(_uut.GetCurrentUsageInMb() > 0);
        }

        [Test]
        public void GetCurrentUsageInMb_AsWorkingSetGrows_ReturnLargerValue()
        {
            int originalValue = _uut.GetCurrentUsageInMb();

            List<Guid> guids = new List<Guid>();
            for (int i = 0; i < 10000000; i++)
                guids.Add(Guid.NewGuid());

            Assert.IsTrue(_uut.GetCurrentUsageInMb() > originalValue);
        }
    }
}
