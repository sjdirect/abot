using System;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class CrawledResultTest
    {
        [TestMethod]
        public void Constructor_ValidUri_CreatesInstance()
        {
            var unitUnderTest = new CrawlResult();
            Assert.AreEqual(default(TimeSpan), unitUnderTest.Elapsed);
            Assert.AreEqual(null, unitUnderTest.ErrorException);
            Assert.AreEqual(false, unitUnderTest.ErrorOccurred);
            Assert.AreEqual(null, unitUnderTest.RootUri);
            Assert.AreEqual(null, unitUnderTest.CrawlContext);
        }

        [TestMethod]
        public void ErrorOccurred_ErrorExceptionNotNull_ReturnsTrue()
        {
            var unitUnderTest = new CrawlResult();
            var ex = new Exception("oh no");
            unitUnderTest.ErrorException = ex;

            Assert.IsTrue(unitUnderTest.ErrorOccurred);
            Assert.AreSame(ex, unitUnderTest.ErrorException);
        }

        [TestMethod]
        public void ErrorOccurred_ErrorExceptionIsNull_ReturnsFalse()
        {
            var unitUnderTest = new CrawlResult();
            unitUnderTest.ErrorException = null;

            Assert.IsFalse(unitUnderTest.ErrorOccurred);
            Assert.IsNull(unitUnderTest.ErrorException);
        }
    }
}
