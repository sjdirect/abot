using Abot2.Poco;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class PageToCrawlTest
    {
        [TestMethod]
        public void Constructor_CreatesInstance()
        {
            var unitUnderTest = new PageToCrawl();
            Assert.AreEqual(false, unitUnderTest.IsRetry);
            Assert.AreEqual(false, unitUnderTest.IsRoot);
            Assert.AreEqual(false, unitUnderTest.IsInternal);
            Assert.AreEqual(null, unitUnderTest.ParentUri);
            Assert.IsNull(unitUnderTest.Uri);
            Assert.AreEqual(0, unitUnderTest.CrawlDepth);
            Assert.IsNull(unitUnderTest.PageBag);
            Assert.IsNull(unitUnderTest.LastRequest);
        }        

        [TestMethod]
        public void Constructor_ValidUri_CreatesInstance()
        {
            var unitUnderTest = new PageToCrawl(new Uri("http://a.com/"));
            Assert.AreEqual(false, unitUnderTest.IsRetry);
            Assert.AreEqual(false, unitUnderTest.IsRoot);
            Assert.AreEqual(false, unitUnderTest.IsInternal);
            Assert.AreEqual(null, unitUnderTest.ParentUri);
            Assert.AreEqual("http://a.com/", unitUnderTest.Uri.AbsoluteUri);
            Assert.AreEqual(0, unitUnderTest.CrawlDepth);
            Assert.IsNotNull(unitUnderTest.PageBag);
            Assert.IsNull(unitUnderTest.LastRequest);
        }

        [TestMethod]
        public void PageBag()
        {
            var unitUnderTest = new PageToCrawl(new Uri("http://a.com/"));
            unitUnderTest.PageBag.SomeVal = "someval";
            unitUnderTest.PageBag.SomeQueue = new Queue<string>();
            unitUnderTest.PageBag.SomeQueue.Enqueue("aaa");
            unitUnderTest.PageBag.SomeQueue.Enqueue("bbb");

            Assert.IsNotNull(unitUnderTest.PageBag);
            Assert.AreEqual("someval", unitUnderTest.PageBag.SomeVal);
            Assert.AreEqual("aaa", unitUnderTest.PageBag.SomeQueue.Dequeue());
            Assert.AreEqual("bbb", unitUnderTest.PageBag.SomeQueue.Dequeue());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_InvalidUri_ThrowsException()
        {
            new PageToCrawl(null);
        }

        [TestMethod]
        public void ToString_MessageHasUri()
        {
            Assert.AreEqual("http://localhost.fiddler:1111/", new PageToCrawl(new Uri("http://localhost.fiddler:1111/")).ToString());
        }
    }
}
