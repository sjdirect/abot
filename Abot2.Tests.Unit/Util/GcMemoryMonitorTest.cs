using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Abot2.Tests.Unit.Util
{
    //Measures process-wide memory, which other tests running at the same time change
    [TestClass]
    [DoNotParallelize]
    public class GcMemoryMonitorTest
    {
        GcMemoryMonitor _uut;

        [TestInitialize]
        public void Setup()
        {
            _uut = new GcMemoryMonitor();
        }

        [TestCleanup]
        public void TearDown()
        {
            if(_uut != null)
                _uut.Dispose();
        }

        [TestMethod]
        public void GetCurrentUsageInMb_ReturnsAValueAboveZero()
        {
            Assert.IsGreaterThan(0, _uut.GetCurrentUsageInMb());
        }

        [TestMethod]
        public void GetCurrentUsageInMb_AsWorkingSetGrows_ReturnLargerValue()
        {
            var originalValue = _uut.GetCurrentUsageInMb();

            var guids = new List<Guid>();
            for (var i = 0; i < 10000000; i++)
                guids.Add(Guid.NewGuid());

            Assert.IsGreaterThan(originalValue, _uut.GetCurrentUsageInMb());
        }
    }
}
