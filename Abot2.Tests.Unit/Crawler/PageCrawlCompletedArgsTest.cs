using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class PageCrawlCompletedArgsTest
    {
        [TestMethod]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            CrawledPage page = new CrawledPage(new Uri("http://aaa.com/"));
            PageCrawlCompletedArgs uut = new PageCrawlCompletedArgs(new CrawlContext(), page);

            Assert.AreSame(page, uut.CrawledPage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArg()
        {
            new PageCrawlCompletedArgs(new CrawlContext(), null);
        }
    }
}
