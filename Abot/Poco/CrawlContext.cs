using Abot.Core;
using System;
using System.Collections.Concurrent;
using System.Dynamic;

namespace Abot.Poco
{
    public class CrawlContext
    {
        public CrawlContext()
        {
            CrawledUrls = new ConcurrentDictionary<string, byte>();
            CrawlCountByDomain = new ConcurrentDictionary<string, int>();
            CrawlBag = new ExpandoObject();
        }

        /// <summary>
        /// The root of the crawl
        /// </summary>
        public Uri RootUri { get; set; }

        /// <summary>
        /// The datetime of the last unsuccessful http status (non 200) was requested
        /// </summary>
        public DateTime CrawlStartDate { get; set; }

        /// <summary>
        /// Threadsafe collection of urls that have been crawled
        /// </summary>
        public ConcurrentDictionary<string, byte> CrawledUrls { get; set; }
        
        /// <summary>
        /// Threadsafe dictionary of domains and how many pages were crawled in that domain
        /// </summary>
        public ConcurrentDictionary<string, int> CrawlCountByDomain { get; set; }

        /// <summary>
        /// Configuration values used to determine crawl settings
        /// </summary>
        public CrawlConfiguration CrawlConfiguration { get; set; }

        /// <summary>
        /// The scheduler that is being used
        /// </summary>
        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// Random dynamic values
        /// </summary>
        public dynamic CrawlBag { get; set; }

        /// <summary>
        /// Whether a request to hard stop the crawl has happened. Will clear all scheduled pages but will allow any threads that are currently crawling to complete.
        /// </summary>
        public bool IsCrawlStopRequested { get; set; }

        /// <summary>
        /// Whether a request to hard stop the crawl has happened. Will clear all scheduled pages and abort any threads that are currently crawling.
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
    }
}
