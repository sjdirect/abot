using Abot.Core;
using NUnit.Framework;
using System.IO;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class OnDiskCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new OnDiskCrawledUrlRepository(new Md5HashGenerator());
        }

        [Test]
        public void Dispose_DeletesCrawledUrlsDirectory()
        {
            string directoryName = "CrawledUrlsTest";
            
            using (OnDiskCrawledUrlRepository unitUnderTest = new OnDiskCrawledUrlRepository(new Md5HashGenerator(), 50, false, directoryName))
            {
                Assert.IsTrue(Directory.Exists(directoryName));
            }

            Assert.IsFalse(Directory.Exists(directoryName));
        }
    }
}
