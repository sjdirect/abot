using Abot.Core;
using Abot.Poco;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class BloomCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new BloomCrawledUrlRepository(new CrawlConfiguration());
        }
    }
}
