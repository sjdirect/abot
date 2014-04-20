using Abot.Core;
using System;
using System.Dynamic;

namespace Abot.Poco
{
    /// <summary>
    /// Holds all implementations of core interfaces that Abot needs to function. 
    /// </summary>
    public class ImplementationContainer
    {
        public ImplementationContainer()
        {
            ImplementationBag = new ExpandoObject();
        }

        public IThreadManager PageRequesterEngineThreadManager { get; set; }

        public IThreadManager PageProcessorEngineThreadManager { get; set; }

        public IHyperLinkParser HyperlinkParser { get; set; }

        public IPageRequester PageRequester { get; set; }

        public ICrawlDecisionMaker CrawlDecisionMaker { get; set; }

        public IMemoryManager MemoryManager { get; set; }

        public IMemoryMonitor MemoryMonitor { get; set; }

        public IPageRequesterEngine PageRequesterEngine { get; set; }

        public IPageProcessorEngine PageProcessorEngine { get; set; }

        public IScheduler<PageToCrawl> PagesToCrawlScheduler { get; set; }

        public IScheduler<PageToCrawl> PagesToProcessScheduler { get; set; }

        public IDomainRateLimiter DomainRateLimiter { get; set; }

        public IRobotsDotTextFinder RobotsDotTextFinder { get; set; }

        public IPolitenessManager PolitenessManager { get; set; }

        public dynamic ImplementationBag { get; set; }


        /// <summary>
        /// Determines whether a page should be crawled or not
        /// </summary>
        public Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPage  { get; set; }

        /// <summary>
        /// Determine whether the page's raw content should be dowloaded
        /// </summary>
        public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContent { get; set; }

        /// <summary>
        /// Determine whether a page's links should be crawled or not
        /// </summary>
        public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldCrawlPageLinks  { get; set; }

        /// <summary>
        /// Determine whether a cerain link on a page should be scheduled to be crawled
        /// </summary>
        public Func<Uri, CrawledPage, CrawlContext, bool> ShouldScheduleLink { get; set; }

        /// <summary>
        /// Determines whether the 1st uri param is considered an internal uri to the second uri param.
        /// </summary>
        public Func<Uri, Uri, bool> IsInternalUri { get; set; }
    }
}
