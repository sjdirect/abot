
using Abot.Core;
using Abot.Poco;
namespace Abot.Crawler
{
    /// <summary>
    /// A crawler that is expected to encounter more then 100k links during a single crawl. Will use on disk collections to optmize for scale. This makes crawls slower than the ShallowCrawler but gives it the ability to crawl a number of links only limitied by the size of the file system.
    /// </summary>
    public class DeepWebCrawler : PoliteWebCrawler
    {
        public DeepWebCrawler()
            : this(null, null, null, null, null, null, null, null, null)
        {
        }

        public DeepWebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester httpRequester,
            IHyperLinkParser hyperLinkParser,
            IMemoryManager memoryManager,
            IDomainRateLimiter domainRateLimiter,
            IRobotsDotTextFinder robotsDotTextFinder)
            : base(crawlConfiguration, crawlDecisionMaker, threadManager, scheduler, httpRequester, hyperLinkParser, memoryManager, null, null)
        {
            _scheduler = scheduler;
            if (_scheduler == null)
                _scheduler = new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, new OnDiskCrawledUrlRepository(new Murmur3HashGenerator()), new FifoPagesToCrawlRepository());
        }
    }
}
