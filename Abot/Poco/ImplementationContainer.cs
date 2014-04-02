using Abot.Core;
using System.Dynamic;

namespace Abot.Poco
{
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

        public dynamic ImplementationBag { get; set; }
    }
}
