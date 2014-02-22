using Abot.Core;
using Commoner.Core.Testing;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    [Obsolete("This will be deleted in the next release with the implementation it tests.")]
    public class InMemoryCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new InMemoryCrawledUrlRepository();
        }

        [Test]
        public void Dispose_SetsInnerCollectionToNull()
        {
            Assert.IsNotNull(ValueHelper.GetFieldValue(_unitUnderTest, "_urlRepository"));

            _unitUnderTest.Dispose();

            Assert.IsNull(ValueHelper.GetFieldValue(_unitUnderTest, "_urlRepository"));
        }
    }
}
