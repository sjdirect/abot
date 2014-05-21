using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Crawler
{
    public class PageActionStartingArgsTest
    {
        [Test]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            PageToCrawl page = new CrawledPage(new Uri("http://aaa.com/"));
            PageActionStartingArgs args = new PageActionStartingArgs(new CrawlContext(), page);

            Assert.AreSame(page, args.PageToCrawl);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArg()
        {
            new PageActionStartingArgs(new CrawlContext(), null);
        }
    }
}
