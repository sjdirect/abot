using Abot.Crawler;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class PageCrawlCompletedArgsTest
    {
        [Test]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            CrawledPage page = new CrawledPage(new Uri("http://aaa.com/"));
            PageCrawlCompletedArgs uut = new PageCrawlCompletedArgs(new CrawlContext(), page);

            Assert.AreSame(page, uut.CrawledPage);
        }

        [Test]
        public void Constructor_NullArg()
        {
            Assert.Throws<ArgumentNullException>(() => new PageCrawlCompletedArgs(new CrawlContext(), null));
        }
    }
}
