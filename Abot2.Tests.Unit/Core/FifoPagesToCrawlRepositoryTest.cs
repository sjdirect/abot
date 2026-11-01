using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
        public void Dispose_ThenAdd_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/"))));
        }

        [TestMethod]
        public void Dispose_ThenGetNext_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _unitUnderTest.GetNext());
        }

        [TestMethod]
        public void Dispose_ThenClear_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _unitUnderTest.Clear());
        }

        [TestMethod]
        public void Dispose_ThenCount_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => _unitUnderTest.Count());
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _unitUnderTest.Dispose();
            _unitUnderTest.Dispose();
        }
    }
}
