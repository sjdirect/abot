using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class PageCrawlDisallowedArgsTest
    {
        PageToCrawl _page = new CrawledPage(new Uri("http://aaa.com/"));
        CrawlContext _context = new CrawlContext();

        [TestMethod]
        public void Constructor_ValidReason_SetsPublicProperty()
        {
            string reason = "aaa";
            PageCrawlDisallowedArgs args = new PageCrawlDisallowedArgs(_context, _page, reason);

            Assert.AreSame(reason, args.DisallowedReason);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullReason()
        {
            new PageCrawlDisallowedArgs(_context, _page, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_EmptyReason()
        {
            new PageCrawlDisallowedArgs(_context, _page, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WhitespaceReason()
        {
            new PageCrawlDisallowedArgs(_context, _page, "   ");
        }
    }
}
