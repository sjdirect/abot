using Abot.Poco;
using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abot.Core
{   
    public interface IPageProcessorEngine: IEngine, IDisposable
    {
        /// <summary>
        /// Synchronous event that is fired before a page is processed.
        /// </summary>
        event EventHandler<PageActionStartingArgs> PageProcessingStarting;

        /// <summary>
        /// Asynchronous event that is fired before a page is processed.
        /// </summary>
        event EventHandler<PageActionStartingArgs> PageProcessingStartingAsync;

        /// <summary>
        /// Synchronous event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageActionCompletedArgs> PageProcessingCompleted;

        /// <summary>
        /// Asynchronous event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageActionCompletedArgs> PageProcessingCompletedAsync;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageActionDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageActionDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageActionDisallowedArgs> PageLinksCrawlDisallowed;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageActionDisallowedArgs> PageLinksCrawlDisallowedAsync;
    }

    /// <summary>
    /// Processes pages from the PagesToProcess scheduler and fires events.
    /// </summary>
    public class PageProcessorEngine : EngineBase, IPageProcessorEngine
    {
        static ILog _logger = LogManager.GetLogger(typeof(PageProcessorEngine).FullName);

        public virtual bool IsDone
        {
            get
            {
                _logger.DebugFormat("IsCancelled: {0}, ThreadsRunning: {1}, PagesToProcess: {2}", CancellationTokenSource.Token.IsCancellationRequested, ImplementationContainer.PageProcessorEngineThreadManager.HasRunningThreads(), CrawlContext.ImplementationContainer.PagesToProcessScheduler.Count);
                return (CancellationTokenSource.Token.IsCancellationRequested ||
                    (!ImplementationContainer.PageProcessorEngineThreadManager.HasRunningThreads() && ImplementationContainer.PagesToProcessScheduler.Count == 0));
            }
        }

        /// <summary>
        /// Synchronous event that is fired before a page is processed.
        /// </summary>
        public event EventHandler<PageActionStartingArgs> PageProcessingStarting;

        /// <summary>
        /// Asynchronous event that is fired before a page is processed.
        /// </summary>
        public event EventHandler<PageActionStartingArgs> PageProcessingStartingAsync;

        /// <summary>
        /// Synchronous event that is fired after a page is processed.
        /// </summary>
        public event EventHandler<PageActionCompletedArgs> PageProcessingCompleted;

        /// <summary>
        /// Asynchronous event that is fired after a page is processed.
        /// </summary>
        public event EventHandler<PageActionCompletedArgs> PageProcessingCompletedAsync;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        public event EventHandler<PageActionDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPage impl returned false. This means the page or its links were not crawled.
        /// </summary>
        public event EventHandler<PageActionDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        public event EventHandler<PageActionDisallowedArgs> PageLinksCrawlDisallowed;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlPageLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        public event EventHandler<PageActionDisallowedArgs> PageLinksCrawlDisallowedAsync;


        public PageProcessorEngine(CrawlConfiguration crawlConfiguration, ImplementationContainer implementationContainer)
            :base(crawlConfiguration, implementationContainer)
        {

        }


        public virtual void Start(CrawlContext crawlContext)
        {
            base.Start(crawlContext);

            _logger.InfoFormat("PageProcessorEngine starting, [{0}] pages left to request", ImplementationContainer.PagesToProcessScheduler.Count);

            //TODO should this task be "LongRunning"
            Task.Factory.StartNew(() =>
            {
                ProcessPages();
            });
        }

        public virtual void Stop()
        {
            _logger.InfoFormat("PageProcessorEngine stopping, [{0}] pages left to process", ImplementationContainer.PagesToProcessScheduler.Count);
            CancellationTokenSource.Cancel();
            Dispose();
        }

        public virtual void Dispose()
        {
            ImplementationContainer.PageProcessorEngineThreadManager.AbortAll();

            //Set all events to null so no more events are fired
            PageProcessingStarting = null;
            PageProcessingStartingAsync = null;
            PageProcessingCompleted = null;
            PageProcessingCompletedAsync = null;
            PageCrawlDisallowed = null;
            PageCrawlDisallowedAsync = null;
            PageLinksCrawlDisallowed = null;
            PageLinksCrawlDisallowedAsync = null;
        }


        protected virtual void ProcessPages()
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (ImplementationContainer.PagesToProcessScheduler.Count > 0)
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    //TODO !!!!!!!!!!!!!!!The scheduler is being bent to handle CrawledPage objects
                    CrawledPage nextPageToProcess = ImplementationContainer.PagesToProcessScheduler.GetNext() as CrawledPage;

                    if (nextPageToProcess == null)
                    {
                        _logger.WarnFormat("PagesToProcessScheduler returning null for GetNext(). Be sure to pass a CrawledPage object to the Add() method");
                    }
                    else
                    {
                        ImplementationContainer.PageProcessorEngineThreadManager.DoWork(() => ProcessPage(nextPageToProcess));
                    }
                }
                else
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    _logger.DebugFormat("Waiting for pages to process...");
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        protected virtual void ProcessPage(CrawledPage crawledPage)
        {
            _logger.DebugFormat("About to process crawled page [{0}], [{1}] crawled pages left to process", crawledPage.Uri, ImplementationContainer.PagesToProcessScheduler.Count);

            base.FirePageActionStartingEventAsync(CrawlContext, PageProcessingStarting, crawledPage, "PageProcessingStartingAsync");
            base.FirePageActionStartingEvent(CrawlContext, PageProcessingStarting, crawledPage, "PageProcessingStarting");

            bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
            if (shouldCrawlPageLinks || CrawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
                ParsePageLinks(crawledPage);

            CancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (shouldCrawlPageLinks)
                SchedulePageLinks(crawledPage);

            CancellationTokenSource.Token.ThrowIfCancellationRequested();

            _logger.InfoFormat("Page processing complete, Url:[{0}] ", crawledPage.Uri.AbsoluteUri);

            base.FirePageActionCompletedEventAsync(CrawlContext, PageProcessingCompletedAsync, crawledPage, "PageProcessingStartingAsync");
            base.FirePageActionCompletedEvent(CrawlContext, PageProcessingCompleted, crawledPage, "PageProcessingStarting");
        }

        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            CrawlDecision shouldCrawlPageLinksDecision = ImplementationContainer.CrawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, CrawlContext);

            if (!shouldCrawlPageLinksDecision.Allow)
            {
                _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);
                base.FirePageActionDisallowedEventAsync(CrawlContext, PageLinksCrawlDisallowedAsync, crawledPage, "PageLinksCrawlDisallowedAsync", shouldCrawlPageLinksDecision.Reason);
                base.FirePageActionDisallowedEvent(CrawlContext, PageLinksCrawlDisallowed, crawledPage, "PageLinksCrawlDisallowed", shouldCrawlPageLinksDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageLinksDecision);
            return shouldCrawlPageLinksDecision.Allow;
        }

        protected virtual bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            CrawlDecision shouldCrawlPageDecision = ImplementationContainer.CrawlDecisionMaker.ShouldCrawlPage(pageToCrawl, CrawlContext);

            if (shouldCrawlPageDecision.Allow)
            {
                AddPageToContext(pageToCrawl);
            }
            else
            {
                _logger.DebugFormat("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
                base.FirePageActionDisallowedEventAsync(CrawlContext, PageCrawlDisallowedAsync, pageToCrawl, "PageCrawlDisallowedAsync", shouldCrawlPageDecision.Reason);
                base.FirePageActionDisallowedEvent(CrawlContext, PageCrawlDisallowed, pageToCrawl, "PageCrawlDisallowed", shouldCrawlPageDecision.Reason);
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
            crawledPage.ParsedLinks = ImplementationContainer.HyperlinkParser.GetLinks(crawledPage);
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
                    page.IsInternal = ImplementationContainer.CrawlDecisionMaker.IsInternal(uri, CrawlContext);
                    page.IsRoot = false;

                    if (ShouldSchedulePageLink(page))
                        CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Add(page);
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
            //TODO Dont need this anymore, use cancellation token
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
