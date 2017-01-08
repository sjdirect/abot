using Abot.Crawler;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Crawler
{
    public class PageCrawlDisallowedArgsTest
    {
        PageToCrawl _page = new CrawledPage(new Uri("http://aaa.com/"));
        CrawlContext _context = new CrawlContext();

        [Test]
        public void Constructor_ValidReason_SetsPublicProperty()
        {
            string reason = "aaa";
            PageCrawlDisallowedArgs args = new PageCrawlDisallowedArgs(_context, _page, reason);

            Assert.AreSame(reason, args.DisallowedReason);
        }

        [Test]
        public void Constructor_NullReason()
        {
            Assert.Throws<ArgumentNullException>(() => new PageCrawlDisallowedArgs(_context, _page, null));
        }

        [Test]
        public void Constructor_EmptyReason()
        {
            Assert.Throws<ArgumentNullException>(() => new PageCrawlDisallowedArgs(_context, _page, ""));
        }

        [Test]
        public void Constructor_WhitespaceReason()
        {
            Assert.Throws<ArgumentNullException>(() => new PageCrawlDisallowedArgs(_context, _page, "   "));
        }
    }
}
