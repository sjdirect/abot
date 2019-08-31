using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class CrawlArgsTest
    {
        [TestMethod]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            CrawledPage page = new CrawledPage(new Uri("http://aaa.com/"));
            CrawlContext context = new CrawlContext();
            CrawlArgs args = new CrawlArgs(context);

            Assert.AreSame(context, args.CrawlContext);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_CrawledPage_IsNull()
        {
            new PageCrawlCompletedArgs(new CrawlContext(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_CrawlContext_IsNull()
        {
            new PageCrawlCompletedArgs(null, new CrawledPage(new Uri("http://aaa.com/")));
        }
    }
}
