using Abot2.Core;
using Abot2.Poco;
using System;
using Robots;
using Abot2.Util;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Abot2.Crawler
{
    /// <summary>
    /// Polite web crawler
    /// </summary>
    public interface IPoliteWebCrawler : IWebCrawler
    {
        /// <summary>
        /// Event occur after robots txt is parsed synchronously
        /// </summary>
        event EventHandler<RobotsDotTextParseCompletedArgs> RobotsDotTextParseCompleted;
    }
    /// <summary>
    /// Extends the WebCrawler class and added politeness features like crawl delays and respecting robots.txt files. 
    /// </summary>
    public class PoliteWebCrawler : WebCrawler, IPoliteWebCrawler
    {
        protected IDomainRateLimiter _domainRateLimiter;
        protected IRobotsDotTextFinder _robotsDotTextFinder;
        protected IRobotsDotText _robotsDotText;

        //What this instance created itself and therefore disposes. Anything passed in is owned by the caller.
        private readonly DomainRateLimiter _ownedDomainRateLimiter;
        private readonly PageRequester _ownedRobotsPageRequester;
        private readonly WebContentExtractor _ownedRobotsContentExtractor;
        private int _isPoliteWebCrawlerDisposed;

        #region Constructors

        public PoliteWebCrawler()
            : this((CrawlConfiguration)null)
        {
        }

        public PoliteWebCrawler(CrawlConfiguration crawlConfiguration)
            : this(crawlConfiguration, null, (IScheduler)null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a crawler instance with custom settings or implementation. Passing in null for any param uses the default implementation.
        /// Implementations passed in are owned by the caller, who must dispose them after this crawler; the ones created for null params are disposed with this crawler.
        /// </summary>
        /// <param name="crawlConfiguration">Configurable crawl values</param>
        /// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
        /// <param name="scheduler">Decides what link should be crawled next</param>
        /// <param name="pageRequester">Makes the raw http requests</param>
        /// <param name="htmlParser">Parses a crawled page for it's hyperlinks</param>
        /// <param name="memoryManager">Checks the memory usage of the host process</param>
        /// <param name="domainRateLimiter">Keeps the minimum delay between requests to the same domain</param>
        /// <param name="robotsDotTextFinder">Finds and parses the site's robots.txt file</param>
        /// <param name="timeProvider">Clock for the crawl and the rate limiting; if null, the system clock (TimeProvider.System)</param>
        public PoliteWebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHtmlParser htmlParser,
            IMemoryManager memoryManager,
            IDomainRateLimiter domainRateLimiter,
            IRobotsDotTextFinder robotsDotTextFinder,
            TimeProvider timeProvider = null)
            : base(crawlConfiguration, crawlDecisionMaker, scheduler, pageRequester, htmlParser, memoryManager, timeProvider)
        {
            InitializePoliteness(domainRateLimiter, robotsDotTextFinder, out _ownedDomainRateLimiter, out _ownedRobotsContentExtractor, out _ownedRobotsPageRequester);
        }

        [Obsolete("IThreadManager is no longer used to run pages. Use the constructor without it; CrawlConfiguration.MaxConcurrentThreads sets how many pages are processed at the same time.")]
        public PoliteWebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHtmlParser htmlParser,
            IMemoryManager memoryManager,
            IDomainRateLimiter domainRateLimiter,
            IRobotsDotTextFinder robotsDotTextFinder)
            : base(crawlConfiguration, crawlDecisionMaker, threadManager, scheduler, pageRequester, htmlParser, memoryManager)
        {
            InitializePoliteness(domainRateLimiter, robotsDotTextFinder, out _ownedDomainRateLimiter, out _ownedRobotsContentExtractor, out _ownedRobotsPageRequester);
        }

        private void InitializePoliteness(IDomainRateLimiter domainRateLimiter, IRobotsDotTextFinder robotsDotTextFinder,
            out DomainRateLimiter ownedDomainRateLimiter, out WebContentExtractor ownedRobotsContentExtractor, out PageRequester ownedRobotsPageRequester)
        {
            ownedDomainRateLimiter = null;
            if (domainRateLimiter == null)
                domainRateLimiter = ownedDomainRateLimiter = new DomainRateLimiter(_crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds, TimeProvider);
            _domainRateLimiter = domainRateLimiter;

            ownedRobotsContentExtractor = null;
            ownedRobotsPageRequester = null;
            if (robotsDotTextFinder == null)
            {
                ownedRobotsContentExtractor = new WebContentExtractor();
                ownedRobotsPageRequester = new PageRequester(_crawlContext.CrawlConfiguration, ownedRobotsContentExtractor, null, TimeProvider);
                robotsDotTextFinder = new RobotsDotTextFinder(ownedRobotsPageRequester);
            }
            _robotsDotTextFinder = robotsDotTextFinder;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isPoliteWebCrawlerDisposed, 1) != 0)
                return;

            //The base stops the crawl threads first, which may still be rate limiting
            base.Dispose(disposing);

            if (disposing)
            {
                _ownedDomainRateLimiter?.Dispose();
                _ownedRobotsPageRequester?.Dispose();
                _ownedRobotsContentExtractor?.Dispose();
            }
        }

        #endregion

        /// <inheritdoc />
        public override async Task<CrawlResult> CrawlAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var robotsDotTextCrawlDelayInSecs = 0;
            var robotsDotTextCrawlDelayInMillisecs = 0;

            //Load robots.txt
            if (_crawlContext.CrawlConfiguration.IsRespectRobotsDotTextEnabled)
            {
                _robotsDotText = await _robotsDotTextFinder.FindAsync(uri, cancellationToken).ConfigureAwait(false);

                if (_robotsDotText != null)
                {
                    FireRobotsDotTextParseCompleted(_robotsDotText.Robots);

                    robotsDotTextCrawlDelayInSecs = _robotsDotText.GetCrawlDelay(_crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);
                    robotsDotTextCrawlDelayInMillisecs = robotsDotTextCrawlDelayInSecs * 1000;
                }
            }

            //Use whichever value is greater between the actual crawl delay value found, the max allowed crawl delay value or the minimum crawl delay required for every domain
            if (robotsDotTextCrawlDelayInSecs > 0 && robotsDotTextCrawlDelayInMillisecs > _crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds)
            {
                if (robotsDotTextCrawlDelayInSecs > _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds)
                {
                    Log.Warning("[{0}] robot.txt file directive [Crawl-delay: {1}] is above the value set in the config value MaxRobotsDotTextCrawlDelay, will use MaxRobotsDotTextCrawlDelay value instead.", uri, _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds);

                    robotsDotTextCrawlDelayInSecs = _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds;
                    robotsDotTextCrawlDelayInMillisecs = robotsDotTextCrawlDelayInSecs * 1000;
                }

                Log.Warning("[{0}] robot.txt file directive [Crawl-delay: {1}] will be respected.", uri, robotsDotTextCrawlDelayInSecs);
                _domainRateLimiter.AddDomain(uri, robotsDotTextCrawlDelayInMillisecs);
            }

            return await base.CrawlAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits, without blocking a thread, until the page's domain may be requested again
        /// </summary>
        protected override Task BeforeRequestAsync(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
        {
            return _domainRateLimiter.RateLimitAsync(pageToCrawl.Uri, cancellationToken);
        }

        /// <inheritdoc />
        public event EventHandler<RobotsDotTextParseCompletedArgs> RobotsDotTextParseCompleted;


        protected override bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            var allowedByRobots = true;
            if (_robotsDotText != null)
                allowedByRobots = _robotsDotText.IsUrlAllowed(pageToCrawl.Uri.AbsoluteUri, _crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);


            //https://github.com/sjdirect/abot/issues/96 Handle scenario where the root is allowed but all the paths below are disallowed like "disallow: /*"
            var allPathsBelowRootAllowedByRobots = false;
            if (_robotsDotText != null && pageToCrawl.IsRoot && allowedByRobots)
            {
                var anyPathOffRoot = pageToCrawl.Uri.AbsoluteUri.EndsWith("/") ? pageToCrawl.Uri.AbsoluteUri + "aaaaa": pageToCrawl.Uri.AbsoluteUri + "/aaaaa";
                allPathsBelowRootAllowedByRobots = _robotsDotText.IsUrlAllowed(anyPathOffRoot, _crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);
            }

            if (_crawlContext.CrawlConfiguration.IsIgnoreRobotsDotTextIfRootDisallowedEnabled && pageToCrawl.IsRoot)    
            {
                if (!allowedByRobots)
                {
                    var message = string.Format("Page [{0}] [Disallowed by robots.txt file], however since IsIgnoreRobotsDotTextIfRootDisallowedEnabled is set to true the robots.txt file will be ignored for this site.", pageToCrawl.Uri.AbsoluteUri);
                    Log.Debug(message);
                    allowedByRobots = true;
                    _robotsDotText = null;
                }
                else if (!allPathsBelowRootAllowedByRobots)
                {
                    var message = string.Format("All Pages below [{0}] [Disallowed by robots.txt file], however since IsIgnoreRobotsDotTextIfRootDisallowedEnabled is set to true the robots.txt file will be ignored for this site.", pageToCrawl.Uri.AbsoluteUri);
                    Log.Debug(message);
                    allowedByRobots = true;
                    _robotsDotText = null;
                }

            }
            else if (!allowedByRobots)
            {
                var message = string.Format("Page [{0}] not crawled, [Disallowed by robots.txt file], set IsRespectRobotsDotText=false in config file if you would like to ignore robots.txt files.", pageToCrawl.Uri.AbsoluteUri);
                Log.Debug(message);

                FirePageCrawlDisallowedEvent(pageToCrawl, message);

                return false;
            }

            return allowedByRobots && base.ShouldCrawlPage(pageToCrawl);
        }

        protected virtual void FireRobotsDotTextParseCompleted(IRobots robots)
        {
            try
            {
                if (RobotsDotTextParseCompleted == null) return;
                RobotsDotTextParseCompleted.Invoke(this, new RobotsDotTextParseCompletedArgs(_crawlContext, robots));
            }
            catch (Exception e)
            {
                Log.Error("An unhandled exception was thrown by a subscriber of the PageLinksCrawlDisallowed event for robots.txt");
                Log.Error(e, "Exception details -->");
            }
        }
    }
}
