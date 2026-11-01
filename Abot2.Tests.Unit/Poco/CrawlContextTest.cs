using System.Collections.Generic;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class CrawlContextTest
    {
        [TestMethod]
        public void Constructor_ValidUri_CreatesInstance()
        {
            var unitUnderTest = new CrawlContext();
            Assert.IsNull(unitUnderTest.RootUri);
            Assert.AreEqual(0, unitUnderTest.CrawledCount);
            Assert.IsNotNull(unitUnderTest.CrawlCountByDomain);
            Assert.IsEmpty(unitUnderTest.CrawlCountByDomain);
            Assert.IsNull(unitUnderTest.CrawlConfiguration);
            Assert.IsNotNull(unitUnderTest.CrawlBag);
            Assert.IsFalse(unitUnderTest.IsCrawlStopRequested);
            Assert.IsNotNull(unitUnderTest.CancellationTokenSource);
        }

        [TestMethod]
        public void CrawlBag()
        {
            var unitUnderTest = new CrawlContext();
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
