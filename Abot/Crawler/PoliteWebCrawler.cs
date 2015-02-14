using Abot.Core;
using Abot.Poco;
using log4net;
using System;

namespace Abot.Crawler
{
    using Abot.Util;
    using System.Threading;

    /// <summary>
    /// Extends the WebCrawler class and added politeness features like crawl delays and respecting robots.txt files. 
    /// </summary>
    [Serializable]
    public class PoliteWebCrawler : WebCrawler
    {
        private static ILog _logger = LogManager.GetLogger("AbotLogger");
        protected IDomainRateLimiter _domainRateLimiter;
        protected IRobotsDotTextFinder _robotsDotTextFinder;
        protected IRobotsDotText _robotsDotText;

        public PoliteWebCrawler()
            : this(null, null, null, null, null, null, null, null, null)
        {
        }

        public PoliteWebCrawler(CrawlConfiguration crawlConfiguration)
            : this(crawlConfiguration, null, null, null, null, null, null, null, null)
        {
        }

        public PoliteWebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHyperLinkParser hyperLinkParser,
            IMemoryManager memoryManager,
            IDomainRateLimiter domainRateLimiter,
            IRobotsDotTextFinder robotsDotTextFinder)
            : base(crawlConfiguration, crawlDecisionMaker, threadManager, scheduler, pageRequester, hyperLinkParser, memoryManager)
        {
            _domainRateLimiter = domainRateLimiter ?? new DomainRateLimiter(_crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds);
            _robotsDotTextFinder = robotsDotTextFinder ?? new RobotsDotTextFinder(new PageRequester(_crawlContext.CrawlConfiguration));
        }

        public override CrawlResult Crawl(Uri uri, CancellationTokenSource cancellationTokenSource)
        {
            int robotsDotTextCrawlDelayInSecs = 0;
            int robotsDotTextCrawlDelayInMillisecs = 0;

            //Load robots.txt
            if (_crawlContext.CrawlConfiguration.IsRespectRobotsDotTextEnabled)
            {
                _robotsDotText = _robotsDotTextFinder.Find(uri);

                if (_robotsDotText != null)
                {
                    robotsDotTextCrawlDelayInSecs = _robotsDotText.GetCrawlDelay(_crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);
                    robotsDotTextCrawlDelayInMillisecs = robotsDotTextCrawlDelayInSecs * 1000;
                }
            }

            //Use whichever value is greater between the actual crawl delay value found, the max allowed crawl delay value or the minimum crawl delay required for every domain
            if (robotsDotTextCrawlDelayInSecs > 0 && robotsDotTextCrawlDelayInMillisecs > _crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds)
            {
                if (robotsDotTextCrawlDelayInSecs > _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds)
                {
                    _logger.WarnFormat("[{0}] robot.txt file directive [Crawl-delay: {1}] is above the value set in the config value MaxRobotsDotTextCrawlDelay, will use MaxRobotsDotTextCrawlDelay value instead.", uri, _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds);

                    robotsDotTextCrawlDelayInSecs = _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds;
                    robotsDotTextCrawlDelayInMillisecs = robotsDotTextCrawlDelayInSecs * 1000;
                }

                _logger.WarnFormat("[{0}] robot.txt file directive [Crawl-delay: {1}] will be respected.", uri, robotsDotTextCrawlDelayInSecs);
                _domainRateLimiter.AddDomain(uri, robotsDotTextCrawlDelayInMillisecs);
            }

            if (robotsDotTextCrawlDelayInSecs > 0 || _crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds > 0)
                PageCrawlStarting += (s, e) => _domainRateLimiter.RateLimit(e.PageToCrawl.Uri);

            return base.Crawl(uri, cancellationTokenSource);
        }

        protected override bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            bool allowedByRobots = true;
            if (_robotsDotText != null)
                allowedByRobots = _robotsDotText.IsUrlAllowed(pageToCrawl.Uri.AbsoluteUri, _crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);

            if (!allowedByRobots)
            {
                string message = string.Format("Page [{0}] not crawled, [Disallowed by robots.txt file], set IsRespectRobotsDotText=false in config file if you would like to ignore robots.txt files.", pageToCrawl.Uri.AbsoluteUri);
                _logger.DebugFormat(message);

                FirePageCrawlDisallowedEventAsync(pageToCrawl, message);
                FirePageCrawlDisallowedEvent(pageToCrawl, message);

                return false;
            }

            return allowedByRobots && base.ShouldCrawlPage(pageToCrawl);
        }
    }
}
