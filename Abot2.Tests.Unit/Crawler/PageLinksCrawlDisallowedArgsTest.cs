using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class PageLinksCrawlDisallowedArgsTest
    {
        CrawledPage _page = new CrawledPage(new Uri("http://aaa.com/"));
        CrawlContext _context = new CrawlContext();

        [TestMethod]
        public void Constructor_ValidReason_SetsPublicProperty()
        {
            string reason = "aaa";
            PageLinksCrawlDisallowedArgs args = new PageLinksCrawlDisallowedArgs(_context, _page, reason);

            Assert.AreSame(reason, args.DisallowedReason);
        }

        [TestMethod]
        public void Constructor_NullReason()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PageLinksCrawlDisallowedArgs(_context, _page, null));
        }

        [TestMethod]
        public void Constructor_EmptyReason()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PageLinksCrawlDisallowedArgs(_context, _page, ""));
        }

        [TestMethod]
        public void Constructor_WhitespaceReason()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PageLinksCrawlDisallowedArgs(_context, _page, "   "));
        }
    }
}
