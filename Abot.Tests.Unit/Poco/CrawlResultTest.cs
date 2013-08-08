using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class CrawledResultTest
    {
        [Test]
        public void Constructor_ValidUri_CreatesInstance()
        {
            CrawlResult unitUnderTest = new CrawlResult();
            Assert.AreEqual(default(TimeSpan), unitUnderTest.Elapsed);
            Assert.AreEqual(null, unitUnderTest.ErrorException);
            Assert.AreEqual(false, unitUnderTest.ErrorOccurred);
            Assert.AreEqual(null, unitUnderTest.RootUri);
            Assert.AreEqual(null, unitUnderTest.CrawlContext);
        }

        [Test]
        public void ErrorOccurred_ErrorExceptionNotNull_ReturnsTrue()
        {
            CrawlResult unitUnderTest = new CrawlResult();
            Exception ex = new Exception("oh no");
            unitUnderTest.ErrorException = ex;

            Assert.IsTrue(unitUnderTest.ErrorOccurred);
            Assert.AreSame(ex, unitUnderTest.ErrorException);
        }

        [Test]
        public void ErrorOccurred_ErrorExceptionIsNull_ReturnsFalse()
        {
            CrawlResult unitUnderTest = new CrawlResult();
            unitUnderTest.ErrorException = null;

            Assert.IsFalse(unitUnderTest.ErrorOccurred);
            Assert.IsNull(unitUnderTest.ErrorException);
        }
    }
}
