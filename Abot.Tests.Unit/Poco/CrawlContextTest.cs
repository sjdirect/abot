using Abot.Poco;
using NUnit.Framework;
using System.Collections.Generic;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class CrawlContextTest
    {
        [Test]
        public void Constructor_ValidUri_CreatesInstance()
        {
            CrawlContext unitUnderTest = new CrawlContext();
            Assert.AreEqual(null, unitUnderTest.RootUri);
            Assert.AreEqual(0, unitUnderTest.CrawledCount);
            Assert.IsNotNull(unitUnderTest.CrawlCountByDomain);
            Assert.AreEqual(0, unitUnderTest.CrawlCountByDomain.Count);
            Assert.IsNull(unitUnderTest.CrawlConfiguration);
            Assert.IsNotNull(unitUnderTest.CrawlBag);
            Assert.AreEqual(false, unitUnderTest.IsCrawlStopRequested);
            Assert.IsNotNull(unitUnderTest.CancellationTokenSource);
        }

        [Test]
        public void CrawlBag()
        {
            CrawlContext unitUnderTest = new CrawlContext();
            unitUnderTest.CrawlBag.SomeVal = "someval";
            unitUnderTest.CrawlBag.SomeQueue = new Queue<string>();
            unitUnderTest.CrawlBag.SomeQueue.Enqueue("aaa");
            unitUnderTest.CrawlBag.SomeQueue.Enqueue("bbb");

            Assert.IsNotNull(unitUnderTest.CrawlBag);
            Assert.AreEqual("someval", unitUnderTest.CrawlBag.SomeVal);
            Assert.AreEqual("aaa", unitUnderTest.CrawlBag.SomeQueue.Dequeue());
            Assert.AreEqual("bbb", unitUnderTest.CrawlBag.SomeQueue.Dequeue());
        }
    }
}
