using Abot.Core;
using Commoner.Core.Testing;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class FifoPagesToCrawlRepositoryTest : PagesToCrawlRepositoryTest
    {
        public override IPagesToCrawlRepository GetInstance()
        {
            return new FifoPagesToCrawlRepository();
        }

        [Test]
        public void Dispose_SetsInnerCollectionToNull()
        {
            Assert.IsNotNull(ValueHelper.GetFieldValue(_unitUnderTest, "_urlQueue"));

            _unitUnderTest.Dispose();

            Assert.IsNull(ValueHelper.GetFieldValue(_unitUnderTest, "_urlQueue"));
        }
    }
}
