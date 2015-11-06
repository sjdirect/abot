using Abot.Core;
using Commoner.Core.Testing;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class CompactCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new CompactCrawledUrlRepository();
        }

    }
}
