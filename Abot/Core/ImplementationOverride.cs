using Abot.Poco;
using System;

namespace Abot.Core
{
    /// <summary>
    /// Creates the default implementations of core interfaces that Abot needs to function
    /// </summary>
    public class ImplementationOverride : ImplementationContainer
    {

        public ImplementationOverride(CrawlConfiguration config)
            :this(config, new ImplementationContainer())
        {

        }


        public ImplementationOverride(CrawlConfiguration config, ImplementationContainer implementationContainer)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            implementationContainer = implementationContainer ?? new ImplementationContainer();

            if (config.MaxMemoryUsageInMb > 0
                || config.MinAvailableMemoryRequiredInMb > 0)
            {
                MemoryMonitor = implementationContainer.MemoryMonitor ?? new CachedMemoryMonitor(new GcMemoryMonitor(), config.MaxMemoryUsageCacheTimeInSeconds);
                MemoryManager = implementationContainer.MemoryManager ?? new MemoryManager(MemoryMonitor);
            }

            //TODO Need a more generic scheduler like ItemScheduler<T>
            PagesToCrawlScheduler = implementationContainer.PagesToCrawlScheduler ?? new PagesToCrawlScheduler(config.IsUriRecrawlingEnabled, null, null);
            PagesToProcessScheduler = implementationContainer.PagesToProcessScheduler ?? new PagesToCrawlScheduler(false, null, null);

            PageRequester = implementationContainer.PageRequester ?? new PageRequester(config);
            CrawlDecisionMaker = implementationContainer.CrawlDecisionMaker ?? new CrawlDecisionMaker();
            HyperlinkParser = implementationContainer.HyperlinkParser ?? new HapHyperLinkParser(config.IsRespectMetaRobotsNoFollowEnabled, config.IsRespectAnchorRelNoFollowEnabled);

            //TODO !! This needs to be 'MaxConcurrentHttpRequestThreads'
            PageRequesterEngineThreadManager = implementationContainer.PageRequesterEngineThreadManager ?? new TaskThreadManager(config.MaxConcurrentThreads);//TODO !! This needs to be 'MaxConcurrentHttpRequestThreads'
            PageProcessorEngineThreadManager = implementationContainer.PageProcessorEngineThreadManager ?? new TaskThreadManager(config.MaxConcurrentThreads);//TODO !! This needs to be 'MaxConcurrentProcessingThreads'

            PageRequesterEngine = implementationContainer.PageRequesterEngine ?? new PageRequesterEngine(config, this);
            PageProcessorEngine = implementationContainer.PageProcessorEngine ?? new PageProcessorEngine(config, this);

            RobotsDotTextFinder = implementationContainer.RobotsDotTextFinder ?? new RobotsDotTextFinder(PageRequester);
            DomainRateLimiter = implementationContainer.DomainRateLimiter ?? new DomainRateLimiter(config.MinCrawlDelayPerDomainMilliSeconds);

            PolitenessManager = implementationContainer.PolitenessManager ?? new PolitenessManager(RobotsDotTextFinder, DomainRateLimiter);
        }
    }
}
