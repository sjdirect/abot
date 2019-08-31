using Abot2.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class BloomFilterCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new BloomFilterCrawledUrlRepository();
        }
    }
}
