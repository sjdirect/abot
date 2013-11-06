
using Abot.Core;
using Abot.Poco;
namespace Abot.Crawler
{
    /// <summary>
    /// A crawler that is NOT expected to encounter more then 100k links during a single crawl. Will use in memory collections to optmize for speed. This makes crawls faster than the DeepCrawler but is limited by the available memory.
    /// </summary>
    public class ShallowWebCrawler : PoliteWebCrawler
    {
        public ShallowWebCrawler()
            : this(null, null, null, null, null, null, null, null, null)
        {
        }

        public ShallowWebCrawler(
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
                _scheduler = new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, new InMemoryCrawledUrlRepository(), new InMemoryPagesToCrawlRepository());
        }
    }
}
