using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Integration
{
    [TestClass]
    public class CrawlTest : CrawlTestBase
    {
        public CrawlTest()
            :base(SetupAssemblyInitializer.Site.BaseUri)
        {

        }

        //The tests that do not test request timeouts have no request timeout instead of the crawler's 15 s,
        //so a slow first request on a busy machine (cold start, mostly on .NET Framework) cannot fail them.
        //Hangs are caught by the test timeout in testconfig.json.
        const int NoCrawlerRequestTimeout = CrawlConfiguration.NoHttpRequestTimeout;

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Crawl_VerifyCrawlResultIsAsExpected()
        {
            var config = new CrawlConfiguration()
            {
                IsExternalPageCrawlingEnabled = true,
                HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout
            };
            await base.CrawlAndAssert(new PoliteWebCrawler(config), TestContext.CancellationToken);
        }

        [TestMethod]
        public async Task Crawl_MaxPagesTo5_OnlyCrawls5Pages()
        {
            var configuration = new CrawlConfiguration
            {
                IsExternalPageCrawlingEnabled = true,
                MaxPagesToCrawl = 5,
                HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout
            };

            var pagesCrawledCount = 0;

            var crawler = new PoliteWebCrawler(configuration);
            crawler.PageCrawlCompleted += (a, b) => Interlocked.Increment(ref pagesCrawledCount);

            await crawler.CrawlAsync(SetupAssemblyInitializer.Site.BaseUri, TestContext.CancellationToken);

            Assert.AreEqual(5, pagesCrawledCount);
        }

        [TestMethod]
        public async Task Crawl_MaxPagesTo25_OnlyCrawls25Pages()
        {
            var configuration = new CrawlConfiguration();
            configuration.MaxPagesToCrawl = 25;
            configuration.HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout;

            var pagesCrawledCount = 0;

            var crawler = new PoliteWebCrawler(configuration);
            crawler.PageCrawlCompleted += (a, b) => Interlocked.Increment(ref pagesCrawledCount);

            await crawler.CrawlAsync(SetupAssemblyInitializer.Site.BaseUri, TestContext.CancellationToken);

            Assert.AreEqual(25, pagesCrawledCount);
        }

        [TestMethod]
        public async Task Crawl_MaxPagesTo5_WithCrawlDelay_OnlyCrawls5Pages()
        {
            var configuration = new CrawlConfiguration();
            configuration.MinCrawlDelayPerDomainMilliSeconds = 1000; //adding delay since it increases the chance of issues with abot crawling more than MaxPagesToCrawl.
            configuration.MaxPagesToCrawl = 5;
            configuration.HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout;

            var pagesCrawledCount = 0;

            var crawler = new PoliteWebCrawler(configuration);
            crawler.PageCrawlCompleted += (a, b) => Interlocked.Increment(ref pagesCrawledCount);

            await crawler.CrawlAsync(SetupAssemblyInitializer.Site.BaseUri, TestContext.CancellationToken);

            Assert.AreEqual(5, pagesCrawledCount);
        }

        [TestMethod]
        public async Task Crawl_CrawlTimeoutIs1Sec_TimesOut()
        {
            var configuration = new CrawlConfiguration();
            configuration.CrawlTimeoutSeconds = 2;
            configuration.HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout;

            var pagesCrawledCount = 0;

            //The site holds every response until the crawl timeout has fired, so the crawl cannot get anywhere before it,
            //however late the timeout fires, and finishes right after it
            using (var site = await TestSite.StartHoldingResponsesAsync(TestContext.CancellationToken))
            {
                var crawler = new TimeoutNotifyingCrawler(configuration, site.ReleaseResponses);
                crawler.PageCrawlCompleted += (a, b) => Interlocked.Increment(ref pagesCrawledCount);

                var result = await crawler.CrawlAsync(site.BaseUri, TestContext.CancellationToken);

                Assert.IsFalse(result.ErrorOccurred);
                Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested, "The crawl timeout should have stopped the crawl");
                Assert.IsLessThan(2, pagesCrawledCount, "Crawled more than 2 pages");
            }
        }

        [TestMethod]
        public async Task Crawl_Synchronous_CancellationTokenCancelled_StopsCrawl()
        {
            //A slow server: the first request is held, the crawl is cancelled while waiting for it, then the response arrives
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var site = await TestSite.StartHoldingResponsesAsync(TestContext.CancellationToken))
            using (var crawler = new PoliteWebCrawler())
            {
                site.RequestReceived += uri =>
                {
                    cancellationTokenSource.Cancel();
                    site.ReleaseResponses();
                };

                var result = await crawler.CrawlAsync(site.BaseUri, cancellationTokenSource.Token);

                Assert.IsTrue(result.ErrorOccurred);
                Assert.IsTrue(result.ErrorException is OperationCanceledException);
            }
        }

        [TestMethod]
        public async Task Crawl_Asynchronous_CancellationTokenCancelled_StopsCrawl()
        {
            //A slow server: the first request is held, the crawl is cancelled while waiting for it, then the response arrives
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var site = await TestSite.StartHoldingResponsesAsync(TestContext.CancellationToken))
            using (var crawler = new PoliteWebCrawler())
            {
                site.RequestReceived += uri =>
                {
                    cancellationTokenSource.Cancel();
                    site.ReleaseResponses();
                };

                //The crawl runs on another task, as when an application starts it in the background
                var result = await Task.Run(() => crawler.CrawlAsync(site.BaseUri, cancellationTokenSource.Token), TestContext.CancellationToken);

                Assert.IsTrue(result.ErrorOccurred);
                Assert.IsTrue(result.ErrorException is OperationCanceledException);
            }
        }

        [TestMethod]
        public async Task Crawl_ServerDoesNotRespondInTime_PageReportsRequestTimeout()
        {
            var configuration = new CrawlConfiguration { HttpRequestTimeoutInSeconds = 1 };
            CrawledPage crawledPage = null;

            //A server that never answers while the crawl runs, so the request can only end by timing out
            using (var site = await TestSite.StartHoldingResponsesAsync(TestContext.CancellationToken))
            {
                var crawler = new PoliteWebCrawler(configuration);
                crawler.PageCrawlCompleted += (s, e) => crawledPage = e.CrawledPage;

                await crawler.CrawlAsync(site.BaseUri, TestContext.CancellationToken);
            }

            Assert.IsNotNull(crawledPage);
            Assert.IsNotNull(crawledPage.HttpRequestException);
            Assert.StartsWith("Request timeout occurred", crawledPage.HttpRequestException.Message);
        }

        [TestMethod]
        public async Task Crawl_IsRateLimited()
        {
            var configuration = new CrawlConfiguration();
            configuration.MaxPagesToCrawl = 3;
            configuration.MinCrawlDelayPerDomainMilliSeconds = 1000; // 1 second * 2 pages = 2 (or more) seconds
            configuration.HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout;

            var pagesCrawledCount = 0;

            var crawler = new PoliteWebCrawler(configuration);
            crawler.PageCrawlCompleted += (a, b) => Interlocked.Increment(ref pagesCrawledCount);

            var uriToCrawl = SetupAssemblyInitializer.Site.BaseUri;
            //A guaranteed minimum: rate limiting really waits. Stopwatch, unlike DateTime.Now, cannot jump
            var timer = System.Diagnostics.Stopwatch.StartNew();
            await crawler.CrawlAsync(uriToCrawl, TestContext.CancellationToken);
            timer.Stop();

            Assert.IsGreaterThan(2000, timer.ElapsedMilliseconds);
            Assert.AreEqual(3, pagesCrawledCount);
        }

        [TestMethod]
        public async Task Crawl_RetryEnabled_VerifyCrawlResultIsAsExpected()
        {
            CrawlConfiguration configuration = new CrawlConfiguration
            {
                MaxRetryCount = 3,
                MinRetryDelayInMilliseconds = 2000,
                IsExternalPageCrawlingEnabled = true,
                HttpRequestTimeoutInSeconds = NoCrawlerRequestTimeout
            };

            await base.CrawlAndAssert(new PoliteWebCrawler(configuration), TestContext.CancellationToken);
        }

        private protected override List<PageResult> GetExpectedCrawlResult()
        {
            return SetupAssemblyInitializer.Site.GetAllPages();
        }

        //Calls back once the crawl timeout has hard-stopped the crawl
        private class TimeoutNotifyingCrawler : PoliteWebCrawler
        {
            readonly Action _onCrawlTimeout;

            public TimeoutNotifyingCrawler(CrawlConfiguration crawlConfiguration, Action onCrawlTimeout)
                : base(crawlConfiguration)
            {
                _onCrawlTimeout = onCrawlTimeout;
            }

            protected override void HandleCrawlTimeout(object sender, System.Timers.ElapsedEventArgs e)
            {
                base.HandleCrawlTimeout(sender, e);
                _onCrawlTimeout();
            }
        }
    }
}
