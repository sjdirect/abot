using Abot.Crawler;
using Abot.Poco;
using log4net;
using System;
using System.Threading;

namespace Abot.Core
{
    public interface IHttpRequestEngine: IDisposable
    {
        /// <summary>
        /// Synchronous event that is fired before a page is crawled.
        /// </summary>
        event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

        /// <summary>
        /// Synchronous event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

        /// <summary>
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// Asynchronous event that is fired before a page is crawled.
        /// </summary>
        event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

        /// <summary>
        /// Asynchronous event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// Whether it is has made http requests for all PagesToCrawl objects.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Handles the multithreading implementation details
        /// </summary>
        IThreadManager ThreadManager { get; set; }

        /// <summary>
        /// Handles managing the priority of what pages need to be crawled
        /// </summary>
        IScheduler Scheduler { get; set; }

        /// <summary>
        /// Handles making http requests
        /// </summary>
        IPageRequester PageRequester { get; set; }

        /// <summary>
        /// Handles parsing hyperlinks our of raw html
        /// </summary>
        IHyperLinkParser HyperLinkParser { get; set; }

        /// <summary>
        /// Determines what pages should be crawled, whether the raw content should be downloaded and if the links on a page should be crawled
        /// </summary>
        ICrawlDecisionMaker CrawlDecisionMaker { get; set; }

        /// <summary>
        /// Registers a delegate to be called to determine whether a page should be crawled or not
        /// </summary>
        Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageShortcut { get; set; }

        /// <summary>
        /// Registers a delegate to be called to determine whether the page's content should be dowloaded
        /// </summary>
        Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContentShortcut { get; set; }

        /// <summary>
        /// Starts the HttpRequestEngine
        /// </summary>
        void Start(CrawlContext crawlContext, CancellationTokenSource cancellationTokenSource);
    }

    public class HttpRequestEngine : IHttpRequestEngine
    {
        static ILog _logger = LogManager.GetLogger(typeof(HttpRequestEngine).FullName);

        //TODO Add comments
        public IThreadManager ThreadManager { get; set; }
        public IScheduler Scheduler { get; set; }
        public IPageRequester PageRequester { get; set; }
        public IHyperLinkParser HyperLinkParser { get; set; }
        public ICrawlDecisionMaker CrawlDecisionMaker { get; set; }
        public bool IsDone
        {
            get { throw new System.NotImplementedException(); }
        }
        public CrawlConfiguration CrawlConfiguration { get; set; }
        public CrawlContext CrawlContext { get; set; }

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a page should be crawled or not
        /// </summary>
        public Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageShortcut { get; set; }

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether the page's content should be dowloaded
        /// </summary>
        public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContentShortcut { get; set; }

        #region Synchronous Events

        /// <summary>
        /// hronous event that is fired before a page is crawled.
        /// </summary>
        public event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

        /// <summary>
        /// hronous event that is fired when an individual page has been crawled.
        /// </summary>
        public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

        /// <summary>
        /// hronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        protected virtual void FirePageCrawlStartingEvent(PageToCrawl pageToCrawl)
        {
            try
            {
                EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStarting;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlStartingArgs(CrawlContext, pageToCrawl));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlStarting event for url:" + pageToCrawl.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

        protected virtual void FirePageCrawlCompletedEvent(CrawledPage crawledPage)
        {
            try
            {
                EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompleted;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlCompletedArgs(CrawlContext, crawledPage));
            }
            catch (Exception e)
            {
                _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

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

        #endregion

        #region Asynchronous Events

        /// <summary>
        /// Asynchronous event that is fired before a page is crawled.
        /// </summary>
        public event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

        /// <summary>
        /// Asynchronous event that is fired when an individual page has been crawled.
        /// </summary>
        public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        protected virtual void FirePageCrawlStartingEventAsync(PageToCrawl pageToCrawl)
        {
            EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStartingAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlStartingArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlStartingArgs(CrawlContext, pageToCrawl), null, null);
                }
            }
        }

        protected virtual void FirePageCrawlCompletedEventAsync(CrawledPage crawledPage)
        {
            EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompletedAsync;

            if (threadSafeEvent == null)
                return;

            if (Scheduler.Count == 0)
            {
                //Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
                try
                {
                    threadSafeEvent(this, new PageCrawlCompletedArgs(CrawlContext, crawledPage));
                }
                catch (Exception e)
                {
                    _logger.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
                    _logger.Error(e);
                }
            }
            else
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlCompletedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlCompletedArgs(CrawlContext, crawledPage), null, null);
                }
            }
        }

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

        public HttpRequestEngine()
            : this(null, null, null, null, null)
        {
            
        }

        public HttpRequestEngine(
            CrawlConfiguration config,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester httpRequester)
        {
            CrawlConfiguration = config ?? this.GetCrawlConfigurationFromConfigFile();
            ThreadManager = threadManager ?? new TaskThreadManager(config.MaxConcurrentThreads > 0 ? config.MaxConcurrentThreads : System.Environment.ProcessorCount);
            Scheduler = scheduler ?? new Scheduler(config.IsUriRecrawlingEnabled, null, null);
            PageRequester = httpRequester ?? new PageRequester(config);
            CrawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();            
        }

        /// <summary>
        /// Starts the HttpRequestEngine
        /// </summary>
        public void Start(CrawlContext crawlContext, CancellationTokenSource cancellationTokenSource)
        {
            //TODO check if null!!!!!
            CrawlContext = crawlContext;
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            Scheduler.Clear();
            ThreadManager.AbortAll();
            Scheduler.Clear();//to be sure nothing was scheduled since first call to clear()

            //Set all events to null so no more events are fired
            PageCrawlStarting = null;
            PageCrawlCompleted = null;
            PageCrawlDisallowed = null;
            //PageLinksCrawlDisallowed = null;
            PageCrawlStartingAsync = null;
            PageCrawlCompletedAsync = null;
            PageCrawlDisallowedAsync = null;
            PageLinksCrawlDisallowedAsync = null;

        }

        private CrawlConfiguration GetCrawlConfigurationFromConfigFile()
        {
            //TODO this was copy/past from CrawlerEngine.cs, find a way to reuse
            AbotConfigurationSectionHandler configFromFile = AbotConfigurationSectionHandler.LoadFromXml();

            if (configFromFile == null)
                throw new InvalidOperationException("abot config section was NOT found");

            _logger.DebugFormat("abot config section was found");
            return configFromFile.Convert();
        }
    }
}
