using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class CrawlDecisionTest
    {
        [TestMethod]
        public void Constructor_ValidUri_CreatesInstance()
        {
            var unitUnderTest = new CrawlDecision();
            Assert.AreEqual(false, unitUnderTest.Allow);
            Assert.AreEqual("", unitUnderTest.Reason);
            Assert.IsFalse(unitUnderTest.ShouldHardStopCrawl);
            Assert.IsFalse(unitUnderTest.ShouldStopCrawl);
        }
    }
}
