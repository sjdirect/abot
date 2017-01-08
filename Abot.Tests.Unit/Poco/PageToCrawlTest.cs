using Abot.Poco;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class PageToCrawlTest
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            PageToCrawl unitUnderTest = new PageToCrawl();
            Assert.AreEqual(false, unitUnderTest.IsRetry);
            Assert.AreEqual(false, unitUnderTest.IsRoot);
            Assert.AreEqual(false, unitUnderTest.IsInternal);
            Assert.AreEqual(null, unitUnderTest.ParentUri);
            Assert.IsNull(unitUnderTest.Uri);
            Assert.AreEqual(0, unitUnderTest.CrawlDepth);
            Assert.IsNull(unitUnderTest.PageBag);
            Assert.IsNull(unitUnderTest.LastRequest);
        }        

        [Test]
        public void Constructor_ValidUri_CreatesInstance()
        {
            PageToCrawl unitUnderTest = new PageToCrawl(new Uri("http://a.com/"));
            Assert.AreEqual(false, unitUnderTest.IsRetry);
            Assert.AreEqual(false, unitUnderTest.IsRoot);
            Assert.AreEqual(false, unitUnderTest.IsInternal);
            Assert.AreEqual(null, unitUnderTest.ParentUri);
            Assert.AreEqual("http://a.com/", unitUnderTest.Uri.AbsoluteUri);
            Assert.AreEqual(0, unitUnderTest.CrawlDepth);
            Assert.IsNotNull(unitUnderTest.PageBag);
            Assert.IsNull(unitUnderTest.LastRequest);
        }

        [Test]
        public void PageBag()
        {
            PageToCrawl unitUnderTest = new PageToCrawl(new Uri("http://a.com/"));
            unitUnderTest.PageBag.SomeVal = "someval";
            unitUnderTest.PageBag.SomeQueue = new Queue<string>();
            unitUnderTest.PageBag.SomeQueue.Enqueue("aaa");
            unitUnderTest.PageBag.SomeQueue.Enqueue("bbb");

            Assert.IsNotNull(unitUnderTest.PageBag);
            Assert.AreEqual("someval", unitUnderTest.PageBag.SomeVal);
            Assert.AreEqual("aaa", unitUnderTest.PageBag.SomeQueue.Dequeue());
            Assert.AreEqual("bbb", unitUnderTest.PageBag.SomeQueue.Dequeue());
        }

        [Test]
        public void Constructor_InvalidUri_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new PageToCrawl(null));
        }

        [Test]
        public void ToString_MessageHasUri()
        {
            Assert.AreEqual("http://localhost.fiddler:1111/", new PageToCrawl(new Uri("http://localhost.fiddler:1111/")).ToString());
        }
    }
}
