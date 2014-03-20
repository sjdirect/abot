using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Threading;

namespace Abot.Poco
{
    public class CrawlContext
    {
        public CrawlContext()
        {
            CrawlCountByDomain = new ConcurrentDictionary<string, int>();
            CrawlBag = new ExpandoObject();
            CancellationTokenSource = new CancellationTokenSource();
            PagesToProcess = new BlockingCollection<CrawledPage>();
        }

        /// <summary>
        /// The root of the crawl
        /// </summary>
        public Uri RootUri { get; set; }

        /// <summary>
        /// total number of pages that have been crawled
        /// </summary>
        public int CrawledCount = 0;

        /// <summary>
        /// The datetime of the last unsuccessful http status (non 200) was requested
        /// </summary>
        public DateTime CrawlStartDate { get; set; }
        
        /// <summary>
        /// Threadsafe dictionary of domains and how many pages were crawled in that domain
        /// </summary>
        public ConcurrentDictionary<string, int> CrawlCountByDomain { get; set; }

        /// <summary>
        /// Configuration values used to determine crawl settings
        /// </summary>
        public CrawlConfiguration CrawlConfiguration { get; set; }

        /// <summary>
        /// Random dynamic values
        /// </summary>
        public dynamic CrawlBag { get; set; }

        /// <summary>
        /// Whether a request to stop the crawl has happened. Will clear all scheduled pages but will allow any threads that are currently crawling to complete.
        /// </summary>
        public bool IsCrawlStopRequested { get; set; }

        /// <summary>
        /// Whether a request to hard stop the crawl has happened. Will clear all scheduled pages and cancel any threads that are currently crawling.
        /// </summary>
        public bool IsCrawlHardStopRequested { get; set; }

        /// <summary>
        /// The memory usage in mb at the start of the crawl
        /// </summary>
        public int MemoryUsageBeforeCrawlInMb { get; set; }

        /// <summary>
        /// The memory usage in mb at the end of the crawl
        /// </summary>
        public int MemoryUsageAfterCrawlInMb { get; set; }

        /// <summary>
        /// Cancellation token used to hard stop the crawl. Will clear all scheduled pages and abort any threads that are currently crawling.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Thread safe collection of pages that need to be processed
        /// </summary>
        public BlockingCollection<CrawledPage> PagesToProcess { get; set; }

        /// <summary>
        /// Contains instances of core implementations used by the crawler and it's dependent components
        /// </summary>
        public ImplementationContainer ImplementationContainer { get; set; }
    }
}
