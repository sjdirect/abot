using Abot.Core;
using Commoner.Core.Testing;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class HashedCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new HashedCrawledUrlRepository();
        }

    }
}
