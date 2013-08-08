using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class PageToCrawlTest
    {
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
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_InvalidUri()
        {
            new PageToCrawl(null);
        }

        [Test]
        public void ToString_MessageHasUri()
        {
            Assert.AreEqual("http://localhost:1111/", new PageToCrawl(new Uri("http://localhost:1111/")).ToString());
        }
    }
}
