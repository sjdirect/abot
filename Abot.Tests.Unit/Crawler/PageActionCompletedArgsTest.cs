using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class PageActionCompletedArgsTest
    {
        [Test]
        public void Constructor_ValidArg_SetsPublicProperty()
        {
            CrawledPage page = new CrawledPage(new Uri("http://aaa.com/"));
            PageActionCompletedArgs uut = new PageActionCompletedArgs(new CrawlContext(), page);

            Assert.AreSame(page, uut.CrawledPage);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArg()
        {
            new PageActionCompletedArgs(new CrawlContext(), null);
        }
    }
}
