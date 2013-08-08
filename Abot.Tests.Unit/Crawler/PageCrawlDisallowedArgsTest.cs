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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullReason()
        {
            new PageCrawlDisallowedArgs(_context, _page, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_EmptyReason()
        {
            new PageCrawlDisallowedArgs(_context, _page, "");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WhitespaceReason()
        {
            new PageCrawlDisallowedArgs(_context, _page, "   ");
        }
    }
}
