using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Poco;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class PageRequesterTest
    {
        public TestContext TestContext { get; set; }

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
        public void Constructor_NullConfig()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PageRequester(null, _fakeWebContentExtractor.Object));
        }

        [TestMethod]
        [DataRow(15, 15)]
        [DataRow(1, 1)]
        public void HttpRequestTimeoutInSeconds_Positive_IsTheClientTimeout(int setting, int expectedSeconds)
        {
            Assert.AreEqual(TimeSpan.FromSeconds(expectedSeconds), BuildClientTimeout(setting));
        }

        [TestMethod]
        public void HttpRequestTimeoutInSeconds_Zero_LeavesHttpClientsDefault()
        {
            using (var defaultClient = new HttpClient())
                Assert.AreEqual(defaultClient.Timeout, BuildClientTimeout(0));
        }

        [TestMethod]
        public void HttpRequestTimeoutInSeconds_MinusOne_MeansNoTimeout()
        {
            Assert.AreEqual(Timeout.InfiniteTimeSpan, BuildClientTimeout(CrawlConfiguration.NoHttpRequestTimeout));
        }

        [TestMethod]
        public void HttpRequestTimeoutInSeconds_BelowMinusOne_IsRejected()
        {
            var config = new CrawlConfiguration { HttpRequestTimeoutInSeconds = -2 };

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PageRequester(config, _fakeWebContentExtractor.Object));
        }

        //Builds the HttpClient the way PageRequester does for a request, without making one
        private TimeSpan BuildClientTimeout(int httpRequestTimeoutInSeconds)
        {
            var config = new CrawlConfiguration { HttpRequestTimeoutInSeconds = httpRequestTimeoutInSeconds };
            using (var unitUnderTest = new ClientBuildingPageRequester(config, _fakeWebContentExtractor.Object))
            using (var client = unitUnderTest.Build())
                return client.Timeout;
        }

        private sealed class ClientBuildingPageRequester : PageRequester
        {
            public ClientBuildingPageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor)
                : base(config, contentExtractor)
            {
            }

            public HttpClient Build() => BuildHttpClient(new HttpClientHandler());
        }

        [TestMethod]
        public void Constructor_NullContentExtractor()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PageRequester(_crawlConfig, null));
        }

        [TestMethod]
        public void Constructor_NullHtmlParserFactory()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, _fakeHttpClient.Object, null, null));
        }

        [TestMethod]
        public async Task MakeRequestAsync_HtmlParserFactoryPassedIn_ParsesThePageWithIt()
        {
            //Arrange
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage()
            };
            _fakeHttpClient.Setup(f => f.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(dummyHttpResponseMessage);
            _fakeWebContentExtractor.Setup(f => f.GetContentAsync(dummyHttpResponseMessage, It.IsAny<CancellationToken>())).ReturnsAsync(new PageContent { Text = "hi there" });
            var fakeHtmlParserFactory = new Mock<IAngleSharpHtmlParserFactory>();
            fakeHtmlParserFactory.Setup(f => f.Create()).Returns(new HtmlParser());

            using (var unitUnderTest = new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, _fakeHttpClient.Object, null, fakeHtmlParserFactory.Object))
            {
                //Act
                var result = await unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

                //Assert
                Assert.AreEqual("hi there", result.AngleSharpHtmlDocument.Body.TextContent);
                fakeHtmlParserFactory.Verify(f => f.Create(), Times.Once());
            }
        }

        [TestMethod]
        public async Task MakeRequestAsync_NullUri()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _unitUnderTest.MakeRequestAsync(null, TestContext.CancellationToken));
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
            _fakeWebContentExtractor.Setup(f => f.GetContentAsync(dummyHttpResponseMessage, It.IsAny<CancellationToken>())).ReturnsAsync(dummyPageContent);

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyAll();

            Assert.IsNull(result.HttpRequestException);

            Assert.AreSame(_validUri, result.Uri);
            Assert.AreSame(dummyHttpRequestMessage, result.HttpRequestMessage);
            Assert.AreSame(dummyHttpResponseMessage, result.HttpResponseMessage);
            Assert.AreSame(dummyPageContent, result.Content);

            Assert.AreNotEqual(default, result.RequestStarted);
            Assert.AreNotEqual(default, result.RequestCompleted);
            Assert.IsNotNull(result.DownloadContentStarted);
            Assert.IsNotNull(result.DownloadContentCompleted);

            Assert.IsLessThanOrEqualTo(result.RequestCompleted, result.RequestStarted);
            Assert.IsTrue(result.DownloadContentStarted <= result.DownloadContentCompleted);
            Assert.IsTrue(result.RequestStarted <= result.DownloadContentStarted);
            Assert.IsTrue(result.RequestCompleted <= result.DownloadContentCompleted);
        }

        //The timestamps are local times, as DateTime.Now gave in 2.x, so for example serializers keep writing their offset
        [TestMethod]
        public async Task MakeRequestAsync_Timestamps_AreLocalTimesFromTheClock()
        {
            var time = new ManualTimeProvider();
            var expected = time.GetLocalNow().DateTime;
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = new HttpRequestMessage() };
            _fakeHttpClient.Setup(f => f.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(dummyHttpResponseMessage);
            _fakeWebContentExtractor.Setup(f => f.GetContentAsync(dummyHttpResponseMessage, It.IsAny<CancellationToken>())).ReturnsAsync(new PageContent());
            using (var unitUnderTest = new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, _fakeHttpClient.Object, time))
            {
                var result = await unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

                foreach (var timestamp in new[] { result.RequestStarted, result.RequestCompleted, result.DownloadContentStarted.Value, result.DownloadContentCompleted.Value })
                {
                    Assert.AreEqual(DateTimeKind.Local, timestamp.Kind);
                    Assert.AreEqual(expected, timestamp);
                }
            }
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
            var result = await _unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

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
            Assert.AreNotEqual(default, result.RequestStarted);
            Assert.AreNotEqual(default, result.RequestCompleted);
            Assert.IsLessThanOrEqualTo(result.RequestCompleted, result.RequestStarted);
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
            var result = await _unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

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
            Assert.StartsWith("Request timeout occurred", result.HttpRequestException.Message);
            Assert.IsNotNull(result.HttpRequestException.InnerException);
            Assert.AreEqual("Oh no timeout", result.HttpRequestException.InnerException.Message);
            Assert.AreNotEqual(default, result.RequestStarted);
            Assert.AreNotEqual(default, result.RequestCompleted);
            Assert.IsLessThanOrEqualTo(result.RequestCompleted, result.RequestStarted);
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
            var result = await _unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

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
            Assert.StartsWith("Unknown error occurred", result.HttpRequestException.Message);
            Assert.AreEqual("Oh no", result.HttpRequestException.InnerException.Message);
            Assert.AreNotEqual(default, result.RequestStarted);
            Assert.AreNotEqual(default, result.RequestCompleted);
            Assert.IsLessThanOrEqualTo(result.RequestCompleted, result.RequestStarted);
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
                .Setup(f => f.GetContentAsync(dummyHttpResponseMessage, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Oh no"));

            //Act
            var result = await _unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

            //Assert
            _fakeHttpClient.VerifyAll();
            _fakeWebContentExtractor.VerifyAll();

            Assert.IsNull(result.HttpRequestException);

            Assert.AreSame(_validUri, result.Uri);
            Assert.AreSame(dummyHttpRequestMessage, result.HttpRequestMessage);
            Assert.AreSame(dummyHttpResponseMessage, result.HttpResponseMessage);
            Assert.IsNotNull(result.Content);

            Assert.AreNotEqual(default, result.RequestStarted);
            Assert.AreNotEqual(default, result.RequestCompleted);
            Assert.IsNotNull(result.DownloadContentStarted);
            Assert.IsNull(result.DownloadContentCompleted);

            Assert.IsLessThanOrEqualTo(result.RequestCompleted, result.RequestStarted);
        }

        [TestMethod]
        public async Task MakeRequestAsync_NoHttpClientInjected_BuildsClientFromConfigAndReturnsExpectedCrawledPageObject()
        {
            //Arrange
            var config = new CrawlConfiguration
            {
                IsSslCertificateValidationEnabled = false,
                IsAlwaysLogin = true,
                LoginUser = "user",
                LoginPassword = "pass",
                IsHttpRequestAutomaticDecompressionEnabled = true,
                IsSendingCookiesEnabled = true,
                HttpProtocolVersion = HttpProtocolVersion.Version10
            };
            const string html = "<html><body>hello</body></html>";
            var unitUnderTest = new InMemoryPageRequester(config, html);

            //Act
            var result = await unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken);

            //Assert
            Assert.IsNull(result.HttpRequestException);
            Assert.AreSame(_validUri, result.Uri);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpResponseMessage.StatusCode);
            Assert.AreEqual(html, result.Content.Text);

            var sentRequest = unitUnderTest.Handler.SentRequest;
            Assert.AreEqual(_validUri, sentRequest.RequestUri);
            Assert.AreEqual(HttpVersion.Version10, sentRequest.Version);
            Assert.IsTrue(sentRequest.Headers.Contains("User-Agent"));
            Assert.AreEqual("Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("user:pass")), sentRequest.Headers.Authorization.ToString());

            var configuredHandler = unitUnderTest.ConfiguredHandler;
            Assert.AreEqual(DecompressionMethods.GZip | DecompressionMethods.Deflate, configuredHandler.AutomaticDecompression);
            Assert.IsTrue(configuredHandler.UseCookies);
#if NETFRAMEWORK
            //Abot's .NET Framework build uses WebRequestHandler, as HttpClientHandler's callback needs .NET Framework 4.7.1
            Assert.IsNotNull(((WebRequestHandler)configuredHandler).ServerCertificateValidationCallback);
#else
            Assert.IsNotNull(configuredHandler.ServerCertificateCustomValidationCallback);
#endif
            Assert.IsNotNull(configuredHandler.Credentials);

            unitUnderTest.Dispose();
            configuredHandler.Dispose();
        }

        /// <summary>
        /// Runs the real handler and client building from config but answers requests in memory so the test never touches the network
        /// </summary>
        private class InMemoryPageRequester : PageRequester
        {
            public InMemoryPageRequester(CrawlConfiguration config, string responseHtml)
                : base(config, new WebContentExtractor())
            {
                Handler = new InMemoryHttpClientHandler(responseHtml);
            }

            public InMemoryHttpClientHandler Handler { get; }

            public HttpClientHandler ConfiguredHandler { get; private set; }

            protected override HttpClientHandler BuildHttpClientHandler(Uri rootUri)
            {
                ConfiguredHandler = base.BuildHttpClientHandler(rootUri);
                return Handler;
            }
        }

        private class InMemoryHttpClientHandler : HttpClientHandler
        {
            private readonly string _responseHtml;

            public InMemoryHttpClientHandler(string responseHtml)
            {
                _responseHtml = responseHtml;
            }

            public HttpRequestMessage SentRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SentRequest = request;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(_responseHtml, Encoding.UTF8, "text/html")
                });
            }
        }

        [TestMethod]
        public void Dispose_DoesNotDisposeHttpClientPassedIn()
        {
            var handler = new DisposeTrackingHandler();
            var httpClient = new HttpClient(handler);
            var unitUnderTest = new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, httpClient);

            unitUnderTest.Dispose();

            Assert.IsFalse(handler.IsDisposed);
            httpClient.Dispose();
            Assert.IsTrue(handler.IsDisposed);
        }

        [TestMethod]
        public async Task MakeRequestAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => _unitUnderTest.MakeRequestAsync(_validUri, TestContext.CancellationToken));
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            _unitUnderTest.Dispose();
            _unitUnderTest.Dispose();
        }

        private class DisposeTrackingHandler : HttpMessageHandler
        {
            public bool IsDisposed { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        [TestMethod]
        public async Task MakeRequestAsync_CancelledWhileWaitingForResponse_IsCancelledInsteadOfReportedAsPageError()
        {
            using (var handler = new NeverRespondingHandler())
            using (var httpClient = new HttpClient(handler))
            using (var unitUnderTest = new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, httpClient))
            using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken))
            {
                var request = unitUnderTest.MakeRequestAsync(_validUri, cancellationTokenSource.Token);
                await handler.RequestReceived;

                cancellationTokenSource.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(() => request);
            }
        }

        [TestMethod]
        public async Task MakeRequestAsync_TokenAlreadyCancelled_DoesNotSendRequest()
        {
            using (var handler = new NeverRespondingHandler())
            using (var httpClient = new HttpClient(handler))
            using (var unitUnderTest = new PageRequester(_crawlConfig, _fakeWebContentExtractor.Object, httpClient))
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(() => unitUnderTest.MakeRequestAsync(_validUri, cancellationTokenSource.Token));
                Assert.IsFalse(handler.RequestReceived.IsCompleted);
            }
        }

        //Never answers: a request only ends when it is cancelled
        private class NeverRespondingHandler : HttpMessageHandler
        {
            readonly TaskCompletionSource<bool> _requestReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task RequestReceived => _requestReceived.Task;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _requestReceived.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable: the delay only ends by cancellation");
            }
        }
    }
}
