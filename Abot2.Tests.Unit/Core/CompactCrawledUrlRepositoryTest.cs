using System;
using Abot2.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class CompactCrawledUrlRepositoryTest : CrawledUrlRepositoryTest
    {
        public override ICrawledUrlRepository GetInstance()
        {
            return new CompactCrawledUrlRepository();
        }

        [TestMethod]
        public void Dispose_ThenContains_ThrowsObjectDisposedException()
        {
            var unitUnderTest = GetInstance();

            unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => unitUnderTest.Contains(new Uri("http://a.com/")));
        }

        //Uris that Uri.Equals considers equal must count as duplicates
        [TestMethod]
        [DataRow("HTTP://A.COM/page", DisplayName = "Scheme and host case")]
        [DataRow("http://a.com:80/page", DisplayName = "Default port")]
        [DataRow("http://a.com/page#section", DisplayName = "Fragment")]
        [DataRow("http://user:pw@a.com/page", DisplayName = "User info")]
        public void AddIfNew_EqualUri_IsDuplicate(string equalUri)
        {
            _unitUnderTest.AddIfNew(new Uri("http://a.com/page"));

            Assert.IsFalse(_unitUnderTest.AddIfNew(new Uri(equalUri)));
        }

        //Uris that Uri.Equals considers different must not count as duplicates
        [TestMethod]
        [DataRow("http://a.com/Page", DisplayName = "Path case")]
        [DataRow("http://a.com/page/", DisplayName = "Trailing slash")]
        [DataRow("http://a.com/page?x=1", DisplayName = "Query")]
        [DataRow("https://a.com/page", DisplayName = "Scheme")]
        [DataRow("http://b.com/page", DisplayName = "Host")]
        public void AddIfNew_DifferentUri_IsNew(string differentUri)
        {
            _unitUnderTest.AddIfNew(new Uri("http://a.com/page"));

            Assert.IsTrue(_unitUnderTest.AddIfNew(new Uri(differentUri)));
        }
    }
}
