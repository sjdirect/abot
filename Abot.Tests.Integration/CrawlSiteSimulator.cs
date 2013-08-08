using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Abot.Tests.Integration
{
    [TestFixture]
    public class CrawlSiteSimulator : CrawlTestBase
    {
        public CrawlSiteSimulator()
            :base(new Uri("http://localhost:1111/"), 15)
        {
            
        }

        [Test]
        public void Crawl_VerifyCrawlResultIsAsExpected()
        {
            new PageRequester(new CrawlConfiguration{ UserAgentString = "aaa" }).MakeRequest(new Uri("http://localhost:1111/PageGenerator/ClearCounters"));
            base.CrawlAndAssert(new PoliteWebCrawler());
        }

        [Test]
        public void Crawl_MaxPagesTo5_OnlyCrawls5Pages()
        {
            new PageRequester(new CrawlConfiguration{ UserAgentString = "aaa" }).MakeRequest(new Uri("http://localhost:1111/PageGenerator/ClearCounters"));
            
            CrawlConfiguration configuration = new CrawlConfiguration();
            configuration.MaxPagesToCrawl = 5;

            int pagesCrawledCount = 0;

            PoliteWebCrawler crawler = new PoliteWebCrawler(configuration, null, null, null, null, null, null, null, null);
            crawler.PageCrawlCompletedAsync += (a, b) => pagesCrawledCount++;

            crawler.Crawl(new Uri("http://localhost:1111/"));

            Assert.AreEqual(5, pagesCrawledCount);
        }

        [Test]
        public void Crawl_MaxPagesTo5_WithCrawlDelay_OnlyCrawls5Pages()
        {
            new PageRequester(new CrawlConfiguration{ UserAgentString = "aaa" }).MakeRequest(new Uri("http://localhost:1111/PageGenerator/ClearCounters"));

            CrawlConfiguration configuration = new CrawlConfiguration();
            configuration.MinCrawlDelayPerDomainMilliSeconds = 1000; //adding delay since it increases the chance of issues with abot crawling more than MaxPagesToCrawl.
            configuration.MaxPagesToCrawl = 5;

            int pagesCrawledCount = 0;

            PoliteWebCrawler crawler = new PoliteWebCrawler(configuration, null, null, null, null, null, null, null, null);
            crawler.PageCrawlCompletedAsync += (a, b) => pagesCrawledCount++;

            crawler.Crawl(new Uri("http://localhost:1111/"));

            Assert.AreEqual(5, pagesCrawledCount);
        }

        protected override List<PageResult> GetExpectedCrawlResult()
        {
            List<PageResult> expectedCrawlResult = new List<PageResult>
            {
                new PageResult { Url = "http://localhost:1111/", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/PageGenerator/Generate?Status200Count=5&Status403Count=1&Status404Count=2&Status500Count=3&Status503Count=4&Page=1", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/PageGenerator/Generate?Status200Count=5&Status403Count=1&Status404Count=2&Status500Count=3&Status503Count=4&Page=3", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/PageGenerator/Generate?Status200Count=5&Status403Count=1&Status404Count=2&Status500Count=3&Status503Count=4&Page=2", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/PageGenerator/Generate?Status200Count=5&Status403Count=1&Status404Count=2&Status500Count=3&Status503Count=4&Page=4", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page1", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page3", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page2", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page4", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page5", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status403/Page1", HttpStatusCode = 403},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page2", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page1", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page1", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page3", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page2", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page2", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page1", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page12", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page11", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page3", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page4", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page13", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page14", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page15", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status403/Page3", HttpStatusCode = 403},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page5", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page6", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page7", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page8", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page6", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page9", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page9", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page7", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page12", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page10", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page11", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page8", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page9", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page10", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status403/Page2", HttpStatusCode = 403},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page3", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page4", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page4", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page5", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page16", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page6", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page6", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page5", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page17", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page18", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page19", HttpStatusCode = 200},
                new PageResult { Url = "http://yahoo.com/", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page8", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status200/Page20", HttpStatusCode = 200},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page7", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status403/Page4", HttpStatusCode = 403},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page7", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page11", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page10", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status404/Page8", HttpStatusCode = 404},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page13", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status500/Page12", HttpStatusCode = 500},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page14", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page15", HttpStatusCode = 503},
                new PageResult { Url = "http://localhost:1111/HttpResponse/Status503/Page16", HttpStatusCode = 503},
                new PageResult { Url = "http://zoogle.com/", HttpStatusCode = 200}
            };

            return expectedCrawlResult;
        }
    }
}
