using Abot.Core;
using NUnit.Framework;
using System;
using System.IO;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class OnDiskCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new OnDiskCrawledUrlRepository(new Md5HashGenerator(), new DirectoryInfo("TestUrls\\" + Guid.NewGuid()), true);
        }

        [Test]
        public void Dispose_DeletesCrawledUrlsDirectory()
        {
            DirectoryInfo directory = new DirectoryInfo("TestUrls");//("TESTTEST");
            using (OnDiskCrawledUrlRepository unitUnderTest = new OnDiskCrawledUrlRepository(new Md5HashGenerator(), directory, true))
            {
                Assert.IsTrue(Directory.Exists(directory.FullName));
            }

            Assert.IsFalse(Directory.Exists(directory.FullName));
        }
    }
}
