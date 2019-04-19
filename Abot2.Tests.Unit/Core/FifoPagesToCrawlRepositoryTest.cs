using Abot2.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class FifoPagesToCrawlRepositoryTest : PagesToCrawlRepositoryTest
    {
        public override IPagesToCrawlRepository GetInstance()
        {
            return new FifoPagesToCrawlRepository();
        }

        [TestMethod]
        public void Dispose_SetsInnerCollectionToNull()
        {
            var aaa = _unitUnderTest as FifoPagesToCrawlRepository;
            Assert.IsNotNull(aaa?.UrlQueue);

            _unitUnderTest.Dispose();

            Assert.IsNull(aaa.UrlQueue);
        }
    }
}
