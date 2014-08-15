using Abot.Crawler;
using Abot.Poco;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Abot.Tests.Integration
{
    [TestFixture]
    public abstract class CrawlTestBase
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        ConcurrentBag<PageResult> _actualCrawledPages = new ConcurrentBag<PageResult>();
        int _maxSecondsToCrawl;
        Uri _rootUri;

        public CrawlTestBase(Uri rootUri, int maxSecondsToCrawl)
        {
            _rootUri = rootUri;
            _maxSecondsToCrawl = maxSecondsToCrawl;
        }

        public void CrawlAndAssert(IWebCrawler crawler)
        {
            crawler.PageCrawlCompletedAsync += crawler_PageCrawlCompleted;

            CrawlResult result = crawler.Crawl(_rootUri);

            Assert.IsNull(result.ErrorException);
            Assert.IsFalse(result.ErrorOccurred);
            Assert.AreSame(_rootUri, result.RootUri);

            List<Discrepancy> descrepancies = GetDescrepancies();
            PrintDescrepancies(descrepancies);

            Assert.AreEqual(0, descrepancies.Count, "There were discrepancies between expected and actual crawl results. See ouput window for details.");
            Assert.IsTrue(result.Elapsed.TotalSeconds < _maxSecondsToCrawl, string.Format("Elapsed Time to crawl {0}, over {1} second threshold", result.Elapsed.TotalSeconds, _maxSecondsToCrawl));
        }

        protected abstract List<PageResult> GetExpectedCrawlResult();

        private void PrintDescrepancies(List<Discrepancy> allDescrepancies)
        {
            if (allDescrepancies.Count < 1)
            {
                _logger.Info("No discrepancies between expected and actual results");
                return;
            }

            IEnumerable<Discrepancy> missingPages = allDescrepancies.Where(d => d.DiscrepencyType == DiscrepencyType.MissingPageFromResult);
            IEnumerable<Discrepancy> unexpectedPages = allDescrepancies.Where(d => d.DiscrepencyType == DiscrepencyType.UnexpectedPageInResult);
            IEnumerable<Discrepancy> unexpectedHttpStatusPages = allDescrepancies.Where(d => d.DiscrepencyType == DiscrepencyType.UnexpectedHttpStatus);

            foreach (Discrepancy discrepancy in missingPages)
            {
                _logger.InfoFormat("Missing:[{0}][{1}]", discrepancy.Expected.Url, discrepancy.Expected.HttpStatusCode);
            }
            foreach (Discrepancy discrepancy in unexpectedHttpStatusPages)
            {
                _logger.InfoFormat("Unexpected Http Status: [{0}] Expected:[{1}] Actual:[{2}]", discrepancy.Actual.Url, discrepancy.Expected.HttpStatusCode, discrepancy.Actual.HttpStatusCode);
            }
            foreach(Discrepancy discrepancy in unexpectedPages)
            {
                _logger.InfoFormat("Unexpected Page:[{0}][{1}]", discrepancy.Actual.Url, discrepancy.Actual.HttpStatusCode);
            }
        }

        private void crawler_PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            PageResult pageResult = new PageResult();
            pageResult.Url = e.CrawledPage.Uri.AbsoluteUri;
            if(e.CrawledPage.HttpWebResponse != null)
                pageResult.HttpStatusCode = Convert.ToInt32(e.CrawledPage.HttpWebResponse.StatusCode);

            _actualCrawledPages.Add(pageResult);
        }

        private List<Discrepancy> GetDescrepancies()
        {
            List<Discrepancy> discrepancies = new List<Discrepancy>();
            List<PageResult> expectedCrawlResult = GetExpectedCrawlResult();

            foreach (PageResult actualPage in _actualCrawledPages)
            {
                Discrepancy discrepancy = ReturnIfIsADiscrepency(expectedCrawlResult.FirstOrDefault(p => p.Url == actualPage.Url), actualPage);
                if (discrepancy != null)
                    discrepancies.Add(discrepancy);
            }

            if (expectedCrawlResult.Count != _actualCrawledPages.Count)
            {
                foreach (PageResult expectedPage in expectedCrawlResult)
                {
                    PageResult expectedPageInActualResult = _actualCrawledPages.FirstOrDefault(a => a.Url == expectedPage.Url);
                    if (expectedPageInActualResult == null)
                        discrepancies.Add(new Discrepancy { Actual = null, Expected = expectedPage, DiscrepencyType = DiscrepencyType.MissingPageFromResult });
                }
            }

            return discrepancies;
        }

        private Discrepancy ReturnIfIsADiscrepency(PageResult expectedPage, PageResult actualPage)
        {
            Discrepancy discrepancy = null;
            if (expectedPage == null)
            {
                discrepancy = new Discrepancy { Actual = actualPage, Expected = null, DiscrepencyType = DiscrepencyType.UnexpectedPageInResult };
            }
            else
            {
                if (expectedPage.HttpStatusCode != actualPage.HttpStatusCode && 
                    (!IsServerUnavailable(expectedPage) &&
                    !IsServerUnavailable(actualPage)) )
                {
                    discrepancy = new Discrepancy { Actual = actualPage, Expected = expectedPage, DiscrepencyType = DiscrepencyType.UnexpectedHttpStatus };
                }
                
            }

            return discrepancy;
        }

        private bool IsServerUnavailable(PageResult page)
        {
            bool isUnavailable = false;

            if (page.HttpStatusCode == 0 ||
                page.HttpStatusCode == 502 ||
                page.HttpStatusCode == 504)
            {
                isUnavailable = true;
            }

            return isUnavailable;
        }
    }

    public class PageResult
    {
        public string Url { get; set; }

        public int HttpStatusCode { get; set; }

        public override bool Equals(object obj)
        {
            PageResult other = obj as PageResult;
            if(other == null)
                return false;

            if (this.Url == other.Url && this.HttpStatusCode == other.HttpStatusCode)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Url + " " + HttpStatusCode;
        }
    }

    public class Discrepancy
    {
        public PageResult Expected { get; set; }

        public PageResult Actual { get; set; }

        public DiscrepencyType DiscrepencyType { get; set; }
    }

    public enum DiscrepencyType
    {
        UnexpectedPageInResult,
        UnexpectedHttpStatus,
        MissingPageFromResult
    }
}
