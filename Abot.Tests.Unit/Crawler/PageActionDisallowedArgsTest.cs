using Abot.Crawler;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Crawler
{
    public class PageActionDisallowedArgsTest
    {
        PageToCrawl _page = new CrawledPage(new Uri("http://aaa.com/"));
        CrawlContext _context = new CrawlContext();

        [Test]
        public void Constructor_ValidReason_SetsPublicProperty()
        {
            string reason = "aaa";
            PageActionDisallowedArgs args = new PageActionDisallowedArgs(_context, _page, reason);

            Assert.AreSame(reason, args.DisallowedReason);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullReason()
        {
            new PageActionDisallowedArgs(_context, _page, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_EmptyReason()
        {
            new PageActionDisallowedArgs(_context, _page, "");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WhitespaceReason()
        {
            new PageActionDisallowedArgs(_context, _page, "   ");
        }
    }
}
