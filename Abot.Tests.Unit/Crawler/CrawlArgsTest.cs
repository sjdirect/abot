using Abot.Crawler;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class CrawlArgsTest
    {
        [Test]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            CrawledPage page = new CrawledPage(new Uri("http://aaa.com/"));
            CrawlContext context = new CrawlContext();
            CrawlArgs args = new CrawlArgs(context);

            Assert.AreSame(context, args.CrawlContext);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_CrawledPage_IsNull()
        {
            new PageCrawlCompletedArgs(new CrawlContext(), null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_CrawlContext_IsNull()
        {
            new PageCrawlCompletedArgs(null, new CrawledPage(new Uri("http://aaa.com/")));
        }
    }
}
