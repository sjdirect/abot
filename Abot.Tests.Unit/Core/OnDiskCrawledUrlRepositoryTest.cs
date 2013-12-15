using Abot.Core;
using NUnit.Framework;
using System.IO;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class OnDiskCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        string _directoryName = "CrawledUrls";

        public override ICrawledUrlRepository GetInstance()
        {
            return new OnDiskCrawledUrlRepository(new Md5HashGenerator());
        }

        [Test]
        public void Dispose_DeletesCrawledUrlsDirectory()
        {
            Assert.IsTrue(Directory.Exists(_directoryName));

            _unitUnderTest.Dispose();

            Assert.IsFalse(Directory.Exists(_directoryName));
        }
    }
}
