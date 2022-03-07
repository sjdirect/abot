using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class PageRequesterWithReuseTest
    {
        CrawlConfiguration _crawlConfig = new CrawlConfiguration()
        {
            ReUseHttpClientInstance = true
        };

        [TestInitialize]
        public void SetUp()
        {
            // Make sure our re-use settings are reset to known state
            PageRequester.DisposeReusedObjects();
        }

        [TestMethod]
        public async Task MakeRequestAsync_RealCall_ReturnsExpectedCrawledPageObject()
        {
            //Arrange
            var unitUnderTest = new PageRequester(
                new CrawlConfiguration()
                {
                    IsSslCertificateValidationEnabled = false,
                    IsAlwaysLogin = true,
                    IsHttpRequestAutomaticDecompressionEnabled = true,
                    IsSendingCookiesEnabled = true,
                    HttpProtocolVersion = HttpProtocolVersion.Version10
                },
                new WebContentExtractor());
            var google = new Uri("https://google.com/");

            //Act
            var result = await unitUnderTest.MakeRequestAsync(google);

            //Assert
            Assert.IsNull(result.HttpRequestException);
            Assert.AreSame(google, result.Uri);
            Assert.IsNotNull(result.HttpRequestMessage);
            Assert.IsNotNull(result.HttpResponseMessage);
            Assert.IsNotNull(result.Content);

            Assert.AreNotEqual("", result.Content.Text);

            unitUnderTest.Dispose();
        }

        [TestMethod]
        public async Task MakeRequestAsync_RealCall_ReUse_ReturnsExpectedCrawledPageObject()
        {
            //Arrange

            // This test will fail if any previous tests used a cached mock http client, so we flush any cached instances
            PageRequester.DisposeReusedObjects();

            var unitUnderTest = new PageRequester(
                new CrawlConfiguration()
                {
                    IsSslCertificateValidationEnabled = false,
                    IsAlwaysLogin = true,
                    IsHttpRequestAutomaticDecompressionEnabled = true,
                    IsSendingCookiesEnabled = true,
                    ReUseHttpClientInstance = true,
                    HttpProtocolVersion = HttpProtocolVersion.Version10
                },
                new WebContentExtractor());
            var google = new Uri("https://google.com/");

            //Act
            // ensure we have triggered use of the existing HTTPClient 
            _ = await unitUnderTest.MakeRequestAsync(google);

            // This request will re-use the previous HttpClient
            var result = await unitUnderTest.MakeRequestAsync(google);

            //Assert
            Assert.IsNull(result.HttpRequestException);
            Assert.AreSame(google, result.Uri);
            Assert.IsNotNull(result.HttpRequestMessage);
            Assert.IsNotNull(result.HttpResponseMessage);
            Assert.IsNotNull(result.Content);

            Assert.AreNotEqual("", result.Content.Text);

            unitUnderTest.Dispose();
        }
    }
}
