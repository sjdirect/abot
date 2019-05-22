using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class PageCrawlStartingArgsTest
    {
        [TestMethod]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            PageToCrawl page = new CrawledPage(new Uri("http://aaa.com/"));
            PageCrawlStartingArgs args = new PageCrawlStartingArgs(new CrawlContext(), page);

            Assert.AreSame(page, args.PageToCrawl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArg()
        {
            new PageCrawlStartingArgs(new CrawlContext(), null);
        }
    }
}
