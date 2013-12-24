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
            Assert.Fail("Directories never get deleted");
            string directoryName = "CrawledUrlsTest";
            OnDiskCrawledUrlRepository unitUnderTest = new OnDiskCrawledUrlRepository(new Md5HashGenerator(), 5000, true, directoryName);

            Assert.IsTrue(Directory.Exists(directoryName));

            _unitUnderTest.Dispose();

            Assert.IsFalse(Directory.Exists(directoryName));
        }
    }
}
