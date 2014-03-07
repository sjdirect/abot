
namespace Abot.Core
{
    using Abot.Poco;
    using log4net;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    
    public interface IPageProcessorEngine: IDisposable
    {
        ///// <summary>
        ///// Synchronous event that is fired before a page is crawled.
        ///// </summary>
        //event EventHandler<PageProcessingStartingArgs> PageProcessingStarting;

        ///// <summary>
        ///// Synchronous event that is fired when an individual page has been crawled.
        ///// </summary>
        //event EventHandler<PageProcessingCompletedArgs> PageProcessingCompleted;

        ///// <summary>
        ///// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        ///// </summary>
        //event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        ///// <summary>
        ///// Asynchronous event that is fired before a page is crawled.
        ///// </summary>
        //event EventHandler<PageProcessingStartingArgs> PageProcessingStartingAsync;

        ///// <summary>
        ///// Asynchronous event that is fired when an individual page has been crawled.
        ///// </summary>
        //event EventHandler<PageProcessingCompletedArgs> PageProcessingCompletedAsync;

        ///// <summary>
        ///// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
        ///// </summary>
        //event EventHandler<PageProcessingDisallowedArgs> PageProcessingDisallowedAsync;

        ///// <summary>
        ///// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        ///// </summary>
        //event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        ///// <summary>
        ///// Synchronous method that registers a delegate to be called to determine whether the 1st uri param is considered an internal uri to the second uri param
        ///// </summary>
        ///// <param name="decisionMaker delegate"></param>
        //Func<Uri, Uri, bool> IsInternalUriShortcut { get; set; }

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

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool IsDone
        {
            get { throw new NotImplementedException(); }
        }

        public void Start(CrawlContext crawlContext)
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

        private void ProcessPage(CrawledPage crawledPage)
        {
            //bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
            //if (shouldCrawlPageLinks || CrawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
            //    ParsePageLinks(crawledPage);

            //ThrowIfCancellationRequested();

            //if (shouldCrawlPageLinks)
            //    SchedulePageLinks(crawledPage);

            //ThrowIfCancellationRequested();

            //FirePageCrawlCompletedEventAsync(crawledPage);
            //FirePageCrawlCompletedEvent(crawledPage);
        }

        public void Stop()
        {
            _logger.InfoFormat("PageProcessorEngine stopping, [{0}] pages left to process", CrawlContext.PagesToProcess.Count);
        }

        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            CrawlDecision shouldCrawlPageLinksDecision = CrawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, CrawlContext);
            if (shouldCrawlPageLinksDecision.Allow)
                shouldCrawlPageLinksDecision = (_shouldCrawlPageLinksDecisionMaker != null) ? _shouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, CrawlContext) : new CrawlDecision { Allow = true };

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
            CrawlDecision shouldCrawlPageDecision = _crawlDecisionMaker.ShouldCrawlPage(pageToCrawl, CrawlContext);
            if (shouldCrawlPageDecision.Allow)
                shouldCrawlPageDecision = (_shouldCrawlPageDecisionMaker != null) ? _shouldCrawlPageDecisionMaker.Invoke(pageToCrawl, CrawlContext) : new CrawlDecision { Allow = true };

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
                if (_shouldScheduleLinkDecisionMaker == null || _shouldScheduleLinkDecisionMaker.Invoke(uri, crawledPage, CrawlContext))
                {
                    try //Added due to a bug in the Uri class related to this (http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed)
                    {
                        PageToCrawl page = new PageToCrawl(uri);
                        page.ParentUri = crawledPage.Uri;
                        page.CrawlDepth = crawledPage.CrawlDepth + 1;
                        page.IsInternal = _isInternalDecisionMaker(uri, CrawlContext.RootUri);
                        page.IsRoot = false;

                        if (ShouldSchedulePageLink(page))
                            CrawlContext.PagesToCrawl.Add(page);
                    }
                    catch { }
                }
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
