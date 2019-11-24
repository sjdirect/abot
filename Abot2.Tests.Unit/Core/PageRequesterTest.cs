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
    public class PageRequesterTest
    {
        PageRequester _unitUnderTest;
        Uri _validUri = new Uri("http://aaa.com/");

        CrawlConfiguration _crawlConfig = new CrawlConfiguration();
        private Mock<IWebContentExtractor> _fakeWebContentExtractor;
        private Mock<HttpClient> _fakeHttpClient;

        [TestInitialize]
        public void SetUp()
        {
            _fakeHttpClient = new Mock<HttpClient>();
            _fakeWebContentExtractor = new Mock<IWebContentExtractor>();

            _unitUnderTest = new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, _fakeHttpClient.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullConfig()
        {
            new PageRequester(null, _fakeWebContentExtractor.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullContentExtractor()
        {
            new PageRequester(_crawlConfig, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task MakeRequestAsync_NullUri()
        {
            await _unitUnderTest.MakeRequestAsync(null);
        }

        [TestMethod]
        public async Task MakeRequestAsync_ValidRequestResponse_ReturnsExpectedCrawledPageObject()
        {
            //Arrange
            var dummyHttpRequestMessage = new HttpRequestMessage();
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = dummyHttpRequestMessage
            };
            var dummyPageContent = new PageContent();
            _fakeHttpClient.Setup(f => 
                f.SendAsync(
                    It.IsAny<HttpRequestMessage>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    dummyHttpResponseMessage);
            _fakeWebContentExtractor.Setup(f => f.GetContentAsync(dummyHttpResponseMessage)).ReturnsAsync(dummyPageContent);

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyAll();

            Assert.IsNull(result.HttpRequestException);

            Assert.AreSame(_validUri, result.Uri);
            Assert.AreSame(dummyHttpRequestMessage, result.HttpRequestMessage);
            Assert.AreSame(dummyHttpResponseMessage, result.HttpResponseMessage);
            Assert.AreSame(dummyPageContent, result.Content);

            Assert.IsNotNull(result.RequestStarted);
            Assert.IsNotNull(result.RequestCompleted);
            Assert.IsNotNull(result.DownloadContentStarted);
            Assert.IsNotNull(result.DownloadContentCompleted);

            Assert.IsTrue(result.RequestStarted < result.RequestCompleted);
            Assert.IsTrue(result.DownloadContentStarted < result.DownloadContentCompleted);
            Assert.IsTrue(result.RequestStarted < result.DownloadContentStarted);
            Assert.IsTrue(result.RequestCompleted < result.DownloadContentCompleted);
        }

        [TestMethod]
        public async Task MakeRequestAsync_HttpRequestResponseThrowsHttpRequestException_ReturnsExpectedCrawledPageObject()
        {
            //Arrange
            _fakeHttpClient.Setup(f =>
                    f.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new HttpRequestException("Oh no"));

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyNoOtherCalls();

            Assert.AreSame(_validUri, result.Uri);

            Assert.IsNull(result.HttpRequestMessage);
            Assert.IsNull(result.HttpResponseMessage);
            Assert.IsNull(result.DownloadContentStarted);
            Assert.IsNull(result.DownloadContentCompleted);

            Assert.IsNotNull(result.Content);
            Assert.IsNull(result.Content.Bytes);
            Assert.IsNull(result.Content.Charset);
            Assert.AreEqual("", result.Content.Text);
            Assert.IsNull(result.Content.Encoding);

            Assert.IsNotNull(result.HttpRequestException);
            Assert.IsNotNull(result.RequestStarted);
            Assert.IsNotNull(result.RequestCompleted);
            Assert.IsTrue(result.RequestStarted < result.RequestCompleted);
        }

        [TestMethod]
        public async Task MakeRequestAsync_HttpRequestResponseTimesOut_ReturnsExpectedCrawledPageObject()
        {
            //Arrange
            _fakeHttpClient.Setup(f =>
                    f.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new TaskCanceledException("Oh no timeout"));

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyNoOtherCalls();

            Assert.AreSame(_validUri, result.Uri);

            Assert.IsNull(result.HttpRequestMessage);
            Assert.IsNull(result.HttpResponseMessage);
            Assert.IsNull(result.DownloadContentStarted);
            Assert.IsNull(result.DownloadContentCompleted);

            Assert.IsNotNull(result.Content);
            Assert.IsNull(result.Content.Bytes);
            Assert.IsNull(result.Content.Charset);
            Assert.AreEqual("", result.Content.Text);
            Assert.IsNull(result.Content.Encoding);

            Assert.IsNotNull(result.HttpRequestException);
            Assert.IsTrue(result.HttpRequestException.Message.StartsWith("Request timeout occurred"));
            Assert.IsNotNull(result.HttpRequestException.InnerException);
            Assert.AreEqual("Oh no timeout", result.HttpRequestException.InnerException.Message);
            Assert.IsNotNull(result.RequestStarted);
            Assert.IsNotNull(result.RequestCompleted);
            Assert.IsTrue(result.RequestStarted < result.RequestCompleted);
        }

        [TestMethod]
        public async Task MakeRequestAsync_HttpRequestResponseThrowsGenericException_ReturnsExpectedCrawledPageObject()
        {
            //Arrange
            _fakeHttpClient.Setup(f =>
                    f.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new Exception("Oh no"));

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyNoOtherCalls();

            Assert.AreSame(_validUri, result.Uri);

            Assert.IsNull(result.HttpRequestMessage);
            Assert.IsNull(result.HttpResponseMessage);
            Assert.IsNull(result.DownloadContentStarted);
            Assert.IsNull(result.DownloadContentCompleted);

            Assert.IsNotNull(result.Content);
            Assert.IsNull(result.Content.Bytes);
            Assert.IsNull(result.Content.Charset);
            Assert.AreEqual("", result.Content.Text);
            Assert.IsNull(result.Content.Encoding);

            Assert.IsNotNull(result.HttpRequestException);
            Assert.IsTrue(result.HttpRequestException.Message.StartsWith("Unknown error occurred"));
            Assert.AreEqual("Oh no", result.HttpRequestException.InnerException.Message);
            Assert.IsNotNull(result.RequestStarted);
            Assert.IsNotNull(result.RequestCompleted);
            Assert.IsTrue(result.RequestStarted < result.RequestCompleted);
        }

        [TestMethod]
        public async Task MakeRequestAsync_WebContentExtractorThrowsException_ReturnsExpectedCrawledPageObject()
        {
            //Arrange
            var dummyHttpRequestMessage = new HttpRequestMessage();
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = dummyHttpRequestMessage
            };
            _fakeHttpClient.Setup(f =>
                    f.SendAsync(
                        It.IsAny<HttpRequestMessage>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    dummyHttpResponseMessage);
            _fakeWebContentExtractor
                .Setup(f => f.GetContentAsync(dummyHttpResponseMessage))
                .ThrowsAsync(new Exception("Oh no"));

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyAll();

            Assert.IsNull(result.HttpRequestException);

            Assert.AreSame(_validUri, result.Uri);
            Assert.AreSame(dummyHttpRequestMessage, result.HttpRequestMessage);
            Assert.AreSame(dummyHttpResponseMessage, result.HttpResponseMessage);
            Assert.IsNotNull(result.Content);

            Assert.IsNotNull(result.RequestStarted);
            Assert.IsNotNull(result.RequestCompleted);
            Assert.IsNotNull(result.DownloadContentStarted);
            Assert.IsNull(result.DownloadContentCompleted);

            Assert.IsTrue(result.RequestStarted < result.RequestCompleted);
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
    }
}
