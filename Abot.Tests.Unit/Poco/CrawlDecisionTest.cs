using Abot.Poco;
using NUnit.Framework;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class CrawlDecisionTest
    {
        [Test]
        public void Constructor_ValidUri_CreatesInstance()
        {
            CrawlDecision unitUnderTest = new CrawlDecision();
            Assert.AreEqual(false, unitUnderTest.Allow);
            Assert.AreEqual("", unitUnderTest.Reason);
            Assert.IsFalse(unitUnderTest.ShouldHardStopCrawl);
            Assert.IsFalse(unitUnderTest.ShouldStopCrawl);
        }
    }
}
