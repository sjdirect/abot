
namespace Abot.Core
{
    using Abot.Crawler;
    using Abot.Poco;
    using log4net;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    
    public interface IPageProcessorEngine: IDisposable
    {
        /// <summary>
        /// Synchronous event that is fired before a page is crawled.
        /// </summary>
        event EventHandler<PageProcessingStartingArgs> PageProcessingStarting;

        /// <summary>
        /// Synchronous event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageProcessingCompletedArgs> PageProcessingCompleted;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        /// <summary>
        /// Asynchronous event that is fired before a page is crawled.
        /// </summary>
        event EventHandler<PageProcessingStartingArgs> PageProcessingStartingAsync;

        /// <summary>
        /// Asynchronous event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageProcessingCompletedArgs> PageProcessingCompletedAsyc;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        /// <summary>
        /// Whether the engine has completed processing all the CrawledPages objects.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Starts processing the content of crawled pages and firing events
        /// </summary>
        /// <param name="crawlContext">The context of the crawl</param>
        void Start(CrawlContext crawlContext);

        /// <summary>
        /// Stops processing crawled pages and firing events
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Makes http requests for all items in the CrawlContext.PagesToCrawl collection and fires events.
    /// </summary>
    public class PageProcessorEngine : IPageProcessorEngine
    {
        static ILog _logger = LogManager.GetLogger(typeof(PageProcessorEngine).FullName);

        /// <summary>
        /// CrawlContext that is used to decide what to pages need to have their content processed
        /// </summary>
        public CrawlContext CrawlContext { get; set; }

        /// <summary>
        /// IThreadManager implementation that is used to manage multithreading
        /// </summary>
        public IThreadManager ThreadManager { get; set; }

        /// <summary>
        /// IHyperlinkParser implementation that is used to parse links from the raw html
        /// </summary>
        public IHyperLinkParser HyperLinkParser { get; set; }

        /// <summary>
        /// ICrawlDecision implementation used to determine what links should be scheduled
        /// </summary>
        public ICrawlDecisionMaker CrawlDecisionMaker { get; set; }

        public bool IsDone
        {
            get
            {
                return (CancellationTokenSource.Token.IsCancellationRequested ||
                    (!ThreadManager.HasRunningThreads() && CrawlContext.PagesToProcess.Count == 0));
            }
        }

        /// <summary>
        /// Cancellation token used to shut down the engine
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Creates instance of PageProcessorEngine using default implemention of dependencies.
        /// </summary>
        public PageProcessorEngine()
            : this(null, null, null, null)
        {
            
        }

        /// <summary>
        /// Creates instance of PageProcessorEngine. Passing null for any value will use the default implementation.
        /// </summary>
        public PageProcessorEngine(
            CrawlConfiguration crawlConfiguration,
            IThreadManager threadManager,
            IHyperLinkParser hyperLinkParser,
            ICrawlDecisionMaker crawlDecisionMaker)
        {
            CrawlConfiguration config = crawlConfiguration ?? new CrawlConfiguration();

            ThreadManager = threadManager ?? new TaskThreadManager(config.MaxConcurrentThreads);
            HyperLinkParser = hyperLinkParser ?? new HapHyperLinkParser(crawlConfiguration.IsRespectMetaRobotsNoFollowEnabled, crawlConfiguration.IsRespectAnchorRelNoFollowEnabled);
            CrawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();
            CancellationTokenSource = new CancellationTokenSource();
        }

        public virtual void Start(CrawlContext crawlContext)
        {
            if (crawlContext == null)
                throw new ArgumentNullException("crawlContext");

            CrawlContext = crawlContext;

            _logger.InfoFormat("PageProcessorEngine starting, [{0}] pages left to request", CrawlContext.PagesToCrawl.Count);

            //TODO should this task be "LongRunning"
            Task.Factory.StartNew(() =>
            {
                foreach (CrawledPage crawledPage in CrawlContext.PagesToProcess.GetConsumingEnumerable())
                {
                    _logger.DebugFormat("About to process crawled page [{0}], [{1}] crawled pages left to process", crawledPage.Uri, CrawlContext.PagesToProcess.Count);
                    
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();
                    ThreadManager.DoWork(() => ProcessPage(crawledPage));
                }
                _logger.DebugFormat("Complete processing crawled pages");
            });
        }

        public virtual void Stop()
        {
            _logger.InfoFormat("PageProcessorEngine stopping, [{0}] pages left to process", CrawlContext.PagesToProcess.Count);
            CancellationTokenSource.Cancel();
            Dispose();
        }

        public virtual void Dispose()
        {
            ThreadManager.AbortAll();

            //Set all events to null so no more events are fired
            PageCrawlDisallowed = null;
            PageLinksCrawlDisallowed = null;
            PageCrawlDisallowedAsync = null;
            PageLinksCrawlDisallowedAsync = null;

        }

        #region Synchronous Events

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        protected virtual void FirePageCrawlDisallowedEvent(PageToCrawl pageToCrawl, string reason)
        {
            try
            {
                EventHandler<PageCrawlDisallowedArgs> threadSafeEvent = PageCrawlDisallowed;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlDisallowedArgs(CrawlContext, pageToCrawl, reason));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlDisallowed event for url:" + pageToCrawl.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

        protected virtual void FirePageLinksCrawlDisallowedEvent(CrawledPage crawledPage, string reason)
        {
            try
            {
                EventHandler<PageLinksCrawlDisallowedArgs> threadSafeEvent = PageLinksCrawlDisallowed;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageLinksCrawlDisallowedArgs(CrawlContext, crawledPage, reason));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageLinksCrawlDisallowed event for url:" + crawledPage.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

        #endregion

        #region Asynchronous Events

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        protected virtual void FirePageCrawlDisallowedEventAsync(PageToCrawl pageToCrawl, string reason)
        {
            EventHandler<PageCrawlDisallowedArgs> threadSafeEvent = PageCrawlDisallowedAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlDisallowedArgs(CrawlContext, pageToCrawl, reason), null, null);
                }
            }
        }

        protected virtual void FirePageLinksCrawlDisallowedEventAsync(CrawledPage crawledPage, string reason)
        {
            EventHandler<PageLinksCrawlDisallowedArgs> threadSafeEvent = PageLinksCrawlDisallowedAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageLinksCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageLinksCrawlDisallowedArgs(CrawlContext, crawledPage, reason), null, null);
                }
            }
        }

        #endregion

        protected virtual void ProcessPage(CrawledPage crawledPage)
        {
            bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
            if (shouldCrawlPageLinks || CrawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
                ParsePageLinks(crawledPage);

            CancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (shouldCrawlPageLinks)
                SchedulePageLinks(crawledPage);

            CancellationTokenSource.Token.ThrowIfCancellationRequested();

            FirePageCrawlCompletedEventAsync(crawledPage);
            FirePageCrawlCompletedEvent(crawledPage);
        }

        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            CrawlDecision shouldCrawlPageLinksDecision = CrawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, CrawlContext);

            if (!shouldCrawlPageLinksDecision.Allow)
            {
                _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);
                FirePageLinksCrawlDisallowedEventAsync(crawledPage, shouldCrawlPageLinksDecision.Reason);
                FirePageLinksCrawlDisallowedEvent(crawledPage, shouldCrawlPageLinksDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageLinksDecision);
            return shouldCrawlPageLinksDecision.Allow;
        }

        protected virtual bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            CrawlDecision shouldCrawlPageDecision = CrawlDecisionMaker.ShouldCrawlPage(pageToCrawl, CrawlContext);

            if (shouldCrawlPageDecision.Allow)
            {
                AddPageToContext(pageToCrawl);
            }
            else
            {
                _logger.DebugFormat("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEventAsync(pageToCrawl, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageDecision);
            return shouldCrawlPageDecision.Allow;
        }

        protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
        {
            if (pageToCrawl.IsRetry)
                return;

            int domainCount = 0;
            Interlocked.Increment(ref CrawlContext.CrawledCount);
            lock (CrawlContext.CrawlCountByDomain)
            {
                if (CrawlContext.CrawlCountByDomain.TryGetValue(pageToCrawl.Uri.Authority, out domainCount))
                    CrawlContext.CrawlCountByDomain[pageToCrawl.Uri.Authority] = domainCount + 1;
                else
                    CrawlContext.CrawlCountByDomain.TryAdd(pageToCrawl.Uri.Authority, 1);
            }
        }

        protected virtual void ParsePageLinks(CrawledPage crawledPage)
        {
            crawledPage.ParsedLinks = HyperLinkParser.GetLinks(crawledPage);
        }

        protected virtual void SchedulePageLinks(CrawledPage crawledPage)
        {
            foreach (Uri uri in crawledPage.ParsedLinks)
            {
                try //Added due to a bug in the Uri class related to this (http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed)
                {
                    PageToCrawl page = new PageToCrawl(uri);
                    page.ParentUri = crawledPage.Uri;
                    page.CrawlDepth = crawledPage.CrawlDepth + 1;
                    page.IsInternal = CrawlDecisionMaker.IsInternal(uri, CrawlContext);
                    page.IsRoot = false;

                    if (ShouldSchedulePageLink(page))
                        CrawlContext.PagesToCrawl.Add(page);
                }
                catch { }
            }
        }

        protected virtual bool ShouldSchedulePageLink(PageToCrawl page)
        {
            if ((page.IsInternal == true || CrawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled == true) && (ShouldCrawlPage(page)))
                return true;

            return false;
        }

        protected virtual void SignalCrawlStopIfNeeded(CrawlDecision decision)
        {
            if (decision.ShouldHardStopCrawl)
            {
                _logger.InfoFormat("Decision marked crawl [Hard Stop] for site [{0}], [{1}]", CrawlContext.RootUri, decision.Reason);
                CrawlContext.IsCrawlHardStopRequested = decision.ShouldHardStopCrawl;
            }
            else if (decision.ShouldStopCrawl)
            {
                _logger.InfoFormat("Decision marked crawl [Stop] for site [{0}], [{1}]", CrawlContext.RootUri, decision.Reason);
                CrawlContext.IsCrawlStopRequested = decision.ShouldStopCrawl;
            }
        }
    }
}
