﻿using Abot.Core;
using Abot.Poco;
using log4net;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Abot.Crawler
{
    using System.Collections.Generic;
    using System.Dynamic;

    public interface IWebCrawler
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
        /// Synchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

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
        /// Asynchronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a page should be crawled or not
        /// </summary>
        void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether the page's content should be dowloaded
        /// </summary>
        /// <param name="shouldDownloadPageContent"></param>
        void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a page's links should be crawled or not
        /// </summary>
        /// <param name="shouldCrawlPageLinksDelegate"></param>
        void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a cerain link on a page should be scheduled to be crawled
        /// </summary>
        void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker);

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether the 1st uri param is considered an internal uri to the second uri param
        /// </summary>
        /// <param name="decisionMaker delegate"></param>
        void IsInternalUri(Func<Uri, Uri, bool> decisionMaker);

        /// <summary>
        /// Begins a crawl using the uri param
        /// </summary>
        CrawlResult Crawl(Uri uri);

        /// <summary>
        /// Begins a crawl using the uri param, and can be cancelled using the CancellationToken
        /// </summary>
        CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource);

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        dynamic CrawlBag { get; set; }
    }

    public abstract class WebCrawler : IWebCrawler
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        protected bool _crawlComplete = false;
        protected bool _crawlStopReported = false;
        protected bool _crawlCancellationReported = false;
        protected System.Timers.Timer _timeoutTimer;
        protected CrawlResult _crawlResult = null;
        protected CrawlContext _crawlContext;
        protected IThreadManager _threadManager;
        protected IScheduler _scheduler;
        protected IPageRequester _httpRequester;
        protected IHyperLinkParser _hyperLinkParser;
        protected ICrawlDecisionMaker _crawlDecisionMaker;
        protected IMemoryManager _memoryManager;
        protected Func<PageToCrawl, CrawlContext, CrawlDecision> _shouldCrawlPageDecisionMaker;
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldDownloadPageContentDecisionMaker;
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldCrawlPageLinksDecisionMaker;
        protected Func<Uri, CrawledPage, CrawlContext, bool> _shouldScheduleLinkDecisionMaker;
        protected Func<Uri, Uri, bool> _isInternalDecisionMaker = (uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        public dynamic CrawlBag { get; set; }

        #region Constructors

        static WebCrawler()
        {
            //This is a workaround for dealing with periods in urls (http://stackoverflow.com/questions/856885/httpwebrequest-to-url-with-dot-at-the-end)
            //Will not be needed when this project is upgraded to 4.5
            MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            FieldInfo flagsField = typeof(UriParser).GetField("m_Flags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (getSyntax != null && flagsField != null)
            {
                foreach (string scheme in new[] { "http", "https" })
                {
                    UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
                    if (parser != null)
                    {
                        int flagsValue = (int)flagsField.GetValue(parser);
                        // Clear the CanonicalizeAsFilePath attribute
                        if ((flagsValue & 0x1000000) != 0)
                            flagsField.SetValue(parser, flagsValue & ~0x1000000);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a crawler instance with the default settings and implementations.
        /// </summary>
        public WebCrawler()
            : this(null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a crawler instance with custom settings or implementation. Passing in null for all params is the equivalent of the empty constructor.
        /// </summary>
        /// <param name="threadManager">Distributes http requests over multiple threads</param>
        /// <param name="scheduler">Decides what link should be crawled next</param>
        /// <param name="httpRequester">Makes the raw http requests</param>
        /// <param name="hyperLinkParser">Parses a crawled page for it's hyperlinks</param>
        /// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
        /// <param name="crawlConfiguration">Configurable crawl values</param>
        public WebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester httpRequester,
            IHyperLinkParser hyperLinkParser,
            IMemoryManager memoryManager)
        {
            _crawlContext = new CrawlContext();
            _crawlContext.CrawlConfiguration = crawlConfiguration ?? GetCrawlConfigurationFromConfigFile();
            CrawlBag = _crawlContext.CrawlBag;

            _threadManager = threadManager ?? new TaskThreadManager(_crawlContext.CrawlConfiguration.MaxConcurrentThreads > 0 ? _crawlContext.CrawlConfiguration.MaxConcurrentThreads : System.Environment.ProcessorCount);
            _scheduler = scheduler ?? new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, null, null);
            _httpRequester = httpRequester ?? new PageRequester(_crawlContext.CrawlConfiguration);
            _crawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();

            if (_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb > 0
                || _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb > 0)
                _memoryManager = memoryManager ?? new MemoryManager(new CachedMemoryMonitor(new GcMemoryMonitor(), _crawlContext.CrawlConfiguration.MaxMemoryUsageCacheTimeInSeconds));

            _hyperLinkParser = hyperLinkParser ?? new HapHyperLinkParser(_crawlContext.CrawlConfiguration.IsRespectMetaRobotsNoFollowEnabled, _crawlContext.CrawlConfiguration.IsRespectAnchorRelNoFollowEnabled);

            _crawlContext.Scheduler = _scheduler;
        }

        #endregion Constructors

        /// <summary>
        /// Begins a synchronous crawl using the uri param, subscribe to events to process data as it becomes available
        /// </summary>
        public virtual CrawlResult Crawl(Uri uri)
        {
            return Crawl(uri, null);
        }

        /// <summary>
        /// Begins a synchronous crawl using the uri param, subscribe to events to process data as it becomes available
        /// </summary>
        public virtual CrawlResult Crawl(Uri uri, CancellationTokenSource cancellationTokenSource)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            _crawlContext.RootUri = uri;

            if (cancellationTokenSource != null)
                _crawlContext.CancellationTokenSource = cancellationTokenSource;

            _crawlResult = new CrawlResult();
            _crawlResult.RootUri = _crawlContext.RootUri;
            _crawlResult.CrawlContext = _crawlContext;
            _crawlComplete = false;

            _logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);

            if (_memoryManager != null)
            {
                _crawlContext.MemoryUsageBeforeCrawlInMb = _memoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageBeforeCrawlInMb);
            }

            PrintConfigValues(_crawlContext.CrawlConfiguration);

            _crawlContext.CrawlStartDate = DateTime.Now;
            Stopwatch timer = Stopwatch.StartNew();

            if (_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds > 0)
            {
                _timeoutTimer = new System.Timers.Timer(_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds * 1000);
                _timeoutTimer.Elapsed += HandleCrawlTimeout;
                _timeoutTimer.Start();
            }

            try
            {
                PageToCrawl rootPage = new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true };
                if (ShouldSchedulePageLink(rootPage))
                    _scheduler.Add(rootPage);

                VerifyRequiredAvailableMemory();
                CrawlSite();
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                _logger.FatalFormat("An error occurred while crawling site [{0}]", uri);
                _logger.Fatal(e);
            }
            finally
            {
                if (_threadManager != null)
                    _threadManager.Dispose();
            }

            if (_timeoutTimer != null)
                _timeoutTimer.Stop();

            timer.Stop();

            if (_memoryManager != null)
            {
                _crawlContext.MemoryUsageAfterCrawlInMb = _memoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageAfterCrawlInMb);
            }

            _crawlResult.Elapsed = timer.Elapsed;
            _logger.InfoFormat("Crawl complete for site [{0}]: Crawled [{1}] pages in [{2}]", _crawlResult.RootUri.AbsoluteUri, _crawlResult.CrawlContext.CrawledCount, _crawlResult.Elapsed);

            return _crawlResult;
        }

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

        /// <summary>
        /// hronous event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        protected virtual void FirePageCrawlStartingEvent(PageToCrawl pageToCrawl)
        {
            try
            {
                EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStarting;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl));
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
                    threadSafeEvent(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
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
                    threadSafeEvent(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason));
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
                    threadSafeEvent(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason));
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
                    del.BeginInvoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl), null, null);
                }
            }
        }

        protected virtual void FirePageCrawlCompletedEventAsync(CrawledPage crawledPage)
        {
            EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompletedAsync;
            
            if (threadSafeEvent == null)
                return;

            if (_scheduler.Count == 0)
            {
                //Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
                try
                {
                    threadSafeEvent(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
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
                    del.BeginInvoke(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage), null, null);
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
                    del.BeginInvoke(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason), null, null);
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
                    del.BeginInvoke(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason), null, null);
                }
            }
        }

        #endregion


        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a page should be crawled or not
        /// </summary>
        public void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldCrawlPageDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether the page's content should be dowloaded
        /// </summary>
        /// <param name="shouldDownloadPageContent"></param>
        public void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldDownloadPageContentDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a page's links should be crawled or not
        /// </summary>
        /// <param name="shouldCrawlPageLinksDelegate"></param>
        public void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldCrawlPageLinksDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether a cerain link on a page should be scheduled to be crawled
        /// </summary>
        public void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker)
        {
            _shouldScheduleLinkDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// Synchronous method that registers a delegate to be called to determine whether the 1st uri param is considered an internal uri to the second uri param
        /// </summary>
        /// <param name="decisionMaker delegate"></param>     
        public void IsInternalUri(Func<Uri, Uri, bool> decisionMaker)
        {
            _isInternalDecisionMaker = decisionMaker;
        }

        public static CrawlConfiguration GetCrawlConfigurationFromConfigFile()
        {
            AbotConfigurationSectionHandler configFromFile = AbotConfigurationSectionHandler.LoadFromXml();

            if (configFromFile == null)
                throw new InvalidOperationException("abot config section was NOT found");

            _logger.DebugFormat("abot config section was found");
            return configFromFile.Convert();
        }

        protected virtual void CrawlSite()
        {
            while (!_crawlComplete)
            {
                RunPreWorkChecks();

                if (_scheduler.Count > 0)
                {
                    _threadManager.DoWork(() => ProcessPage(_scheduler.GetNext()));
                }
                else if (!_threadManager.HasRunningThreads())
                {
                    _crawlComplete = true;
                }
                else
                {
                    _logger.DebugFormat("Waiting for links to be scheduled...");
                    System.Threading.Thread.Sleep(2500);
                }
            }
        }

        protected virtual void VerifyRequiredAvailableMemory()
        {
            if (_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb < 1)
                return;

            if (!_memoryManager.IsSpaceAvailable(_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
                throw new InsufficientMemoryException(string.Format("Process does not have the configured [{0}mb] of available memory to crawl site [{1}]. This is configurable through the minAvailableMemoryRequiredInMb in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.", _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb, _crawlContext.RootUri));
        }

        protected virtual void RunPreWorkChecks()
        {
            CheckMemoryUsage();
            CheckForCancellationRequest();
            CheckForHardStopRequest();
            CheckForStopRequest();
        }

        protected virtual void CheckMemoryUsage()
        {
            if (_memoryManager == null
                || _crawlContext.IsCrawlHardStopRequested
                || _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb < 1)
                return;

            int currentMemoryUsage = _memoryManager.GetCurrentUsageInMb();
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("Current memory usage for site [{0}] is [{1}mb]", _crawlContext.RootUri, currentMemoryUsage);

            if (currentMemoryUsage > _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
            {
                _memoryManager.Dispose();
                _memoryManager = null;

                string message = string.Format("Process is using [{0}mb] of memory which is above the max configured of [{1}mb] for site [{2}]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", currentMemoryUsage, _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb, _crawlContext.RootUri);
                _crawlResult.ErrorException = new InsufficientMemoryException(message);

                _logger.Fatal(_crawlResult.ErrorException);
                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }

        protected virtual void CheckForCancellationRequest()
        {
            if (_crawlContext.CancellationTokenSource.IsCancellationRequested)
            {
                if (!_crawlCancellationReported)
                {
                    string message = string.Format("Crawl cancellation requested for site [{0}]!", _crawlContext.RootUri);
                    _logger.Fatal(message);
                    _crawlResult.ErrorException = new OperationCanceledException(message, _crawlContext.CancellationTokenSource.Token);
                    _crawlContext.IsCrawlHardStopRequested = true;
                    _crawlCancellationReported = true;
                }
            }
        }

        protected virtual void CheckForHardStopRequest()
        {
            if (_crawlContext.IsCrawlHardStopRequested)
            {
                if (!_crawlStopReported)
                {
                    _logger.InfoFormat("Hard crawl stop requested for site [{0}]!", _crawlContext.RootUri);
                    _crawlStopReported = true;
                }

                _scheduler.Clear();
                _threadManager.AbortAll();
                _scheduler.Clear();//to be sure nothing was scheduled since first call to clear()

                //Set all events to null so no more events are fired
                PageCrawlStarting = null;
                PageCrawlCompleted = null;
                PageCrawlDisallowed = null;
                PageLinksCrawlDisallowed = null;
                PageCrawlStartingAsync = null;
                PageCrawlCompletedAsync = null;
                PageCrawlDisallowedAsync = null;
                PageLinksCrawlDisallowedAsync = null;
            }
        }

        protected virtual void CheckForStopRequest()
        {
            if (_crawlContext.IsCrawlStopRequested)
            {
                if (!_crawlStopReported)
                {
                    _logger.InfoFormat("Crawl stop requested for site [{0}]!", _crawlContext.RootUri);
                    _crawlStopReported = true;
                }
                _scheduler.Clear();
            }
        }

        protected virtual void HandleCrawlTimeout(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer elapsedTimer = sender as System.Timers.Timer;
            if (elapsedTimer != null)
                elapsedTimer.Stop();

            _logger.InfoFormat("Crawl timeout of [{0}] seconds has been reached for [{1}]", _crawlContext.CrawlConfiguration.CrawlTimeoutSeconds, _crawlContext.RootUri);
            _crawlContext.IsCrawlHardStopRequested = true;
        }

        protected virtual void ProcessPage(PageToCrawl pageToCrawl)
        {
            try
            {
                if (pageToCrawl == null)
                    return;

                ThrowIfCancellationRequested();

                AddPageToContext(pageToCrawl);
                CrawledPage crawledPage = CrawlThePage(pageToCrawl);
                
                if (PageSizeIsAboveMax(crawledPage))
                    return;

                ThrowIfCancellationRequested();

                bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
                if (shouldCrawlPageLinks || _crawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
                    ParsePageLinks(crawledPage);

                ThrowIfCancellationRequested();

                if (shouldCrawlPageLinks)
                    SchedulePageLinks(crawledPage);

                ThrowIfCancellationRequested();

                FirePageCrawlCompletedEventAsync(crawledPage);
                FirePageCrawlCompletedEvent(crawledPage);
            }
            catch (OperationCanceledException oce)
            {
                _logger.DebugFormat("Thread cancelled while crawling/processing page [{0}]", pageToCrawl.Uri);
                throw;
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                _logger.FatalFormat("Error occurred during processing of page [{0}]", pageToCrawl.Uri);
                _logger.Fatal(e);

                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }

        protected virtual void ThrowIfCancellationRequested()
        {
            if (_crawlContext.CancellationTokenSource != null && _crawlContext.CancellationTokenSource.IsCancellationRequested)
                _crawlContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        protected virtual bool PageSizeIsAboveMax(CrawledPage crawledPage)
        {
            bool isAboveMax = false;
            if (_crawlContext.CrawlConfiguration.MaxPageSizeInBytes > 0 &&
                crawledPage.Content.Bytes != null && 
                crawledPage.Content.Bytes.Length > _crawlContext.CrawlConfiguration.MaxPageSizeInBytes)
            {
                isAboveMax = true;
                _logger.DebugFormat("Page [{0}] has a page size of [{1}] bytes which is above the [{2}] byte max", crawledPage.Uri, crawledPage.Content.Bytes.Length, _crawlContext.CrawlConfiguration.MaxPageSizeInBytes);
            }
            return isAboveMax;
        }

        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            CrawlDecision shouldCrawlPageLinksDecision = _crawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, _crawlContext);
            if (shouldCrawlPageLinksDecision.Allow)
                shouldCrawlPageLinksDecision = (_shouldCrawlPageLinksDecisionMaker != null) ? _shouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

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
            CrawlDecision shouldCrawlPageDecision = _crawlDecisionMaker.ShouldCrawlPage(pageToCrawl, _crawlContext);
            if (shouldCrawlPageDecision.Allow)
                shouldCrawlPageDecision = (_shouldCrawlPageDecisionMaker != null) ? _shouldCrawlPageDecisionMaker.Invoke(pageToCrawl, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldCrawlPageDecision.Allow)
            {
                _logger.DebugFormat("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEventAsync(pageToCrawl, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageDecision);
            return shouldCrawlPageDecision.Allow;
        }

        protected virtual CrawledPage CrawlThePage(PageToCrawl pageToCrawl)
        {
            _logger.DebugFormat("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);
            FirePageCrawlStartingEventAsync(pageToCrawl);
            FirePageCrawlStartingEvent(pageToCrawl);

            CrawledPage crawledPage = _httpRequester.MakeRequest(pageToCrawl.Uri, (x) => ShouldDownloadPageContentWrapper(x));
            dynamic combinedPageBag = this.CombinePageBags(pageToCrawl.PageBag, crawledPage.PageBag);
            AutoMapper.Mapper.CreateMap<PageToCrawl, CrawledPage>();
            AutoMapper.Mapper.Map(pageToCrawl, crawledPage);
            crawledPage.PageBag = combinedPageBag;

            if (crawledPage.HttpWebResponse == null)
                _logger.InfoFormat("Page crawl complete, Status:[NA] Url:[{0}] Parent:[{1}]", crawledPage.Uri.AbsoluteUri, crawledPage.ParentUri);
            else
                _logger.InfoFormat("Page crawl complete, Status:[{0}] Url:[{1}] Parent:[{2}]", Convert.ToInt32(crawledPage.HttpWebResponse.StatusCode), crawledPage.Uri.AbsoluteUri, crawledPage.ParentUri);

            return crawledPage;
        }

        protected virtual dynamic CombinePageBags(dynamic pageToCrawlBag, dynamic crawledPageBag )
        {
            IDictionary<string, object> combinedBag = new ExpandoObject();
            var pageToCrawlBagDict = pageToCrawlBag as IDictionary<string, object>;
            var crawledPageBagDict = crawledPageBag as IDictionary<string, object>;
            
            foreach (KeyValuePair<string, object> entry in pageToCrawlBagDict) combinedBag[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, object> entry in crawledPageBagDict) combinedBag[entry.Key] = entry.Value;

            return combinedBag;
        }

        protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
        {
            if (pageToCrawl.IsRetry)
                return;

            int domainCount = 0;
            Interlocked.Increment(ref _crawlContext.CrawledCount);
            lock (_crawlContext.CrawlCountByDomain)
            {
                if (_crawlContext.CrawlCountByDomain.TryGetValue(pageToCrawl.Uri.Authority, out domainCount))
                    _crawlContext.CrawlCountByDomain[pageToCrawl.Uri.Authority] = domainCount + 1;
                else
                    _crawlContext.CrawlCountByDomain.TryAdd(pageToCrawl.Uri.Authority, 1);
            }
        }

        protected virtual void ParsePageLinks(CrawledPage crawledPage)
        {
            crawledPage.ParsedLinks = _hyperLinkParser.GetLinks(crawledPage);
        }

        protected virtual void SchedulePageLinks(CrawledPage crawledPage)
        {
            foreach (Uri uri in crawledPage.ParsedLinks)
            {
                if (_shouldScheduleLinkDecisionMaker == null || _shouldScheduleLinkDecisionMaker.Invoke(uri, crawledPage, _crawlContext))
                {
                    try //Added due to a bug in the Uri class related to this (http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed)
                    {
                        PageToCrawl page = new PageToCrawl(uri);
                        page.ParentUri = crawledPage.Uri;
                        page.CrawlDepth = crawledPage.CrawlDepth + 1;
                        page.IsInternal = _isInternalDecisionMaker(uri, _crawlContext.RootUri);
                        page.IsRoot = false;

                        if (ShouldSchedulePageLink(page))
                        {
                            _scheduler.Add(page);
                        }
                    }
                    catch { }
                }
            }
        }

        protected virtual bool ShouldSchedulePageLink(PageToCrawl page)
        {
            if ((page.IsInternal == true || _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled == true) && (ShouldCrawlPage(page)))
                return true;

            return false;   
        }

        protected virtual CrawlDecision ShouldDownloadPageContentWrapper(CrawledPage crawledPage)
        {
            CrawlDecision decision = _crawlDecisionMaker.ShouldDownloadPageContent(crawledPage, _crawlContext);
            if (decision.Allow)
                decision = (_shouldDownloadPageContentDecisionMaker != null) ? _shouldDownloadPageContentDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            SignalCrawlStopIfNeeded(decision);
            return decision;
        }

        protected virtual void PrintConfigValues(CrawlConfiguration config)
        {
            _logger.Info("Configuration Values:");

            string indentString = new string(' ', 2);
            foreach (PropertyInfo property in config.GetType().GetProperties())
            {
                if (property.Name != "ConfigurationExtensions")
                    _logger.InfoFormat("{0}{1}: {2}", indentString, property.Name, property.GetValue(config, null));
            }

            foreach (string key in config.ConfigurationExtensions.Keys)
            {
                _logger.InfoFormat("{0}{1}: {2}", indentString, key, config.ConfigurationExtensions[key]);
            }
        }

        protected virtual void SignalCrawlStopIfNeeded(CrawlDecision decision)
        {
            if (decision.ShouldHardStopCrawl)
            {
                _logger.InfoFormat("Decision marked crawl [Hard Stop] for site [{0}], [{1}]", _crawlContext.RootUri, decision.Reason);
                _crawlContext.IsCrawlHardStopRequested = decision.ShouldHardStopCrawl;
            }
            else if (decision.ShouldStopCrawl)
            {
                _logger.InfoFormat("Decision marked crawl [Stop] for site [{0}], [{1}]", _crawlContext.RootUri, decision.Reason);
                _crawlContext.IsCrawlStopRequested = decision.ShouldStopCrawl;
            }
        }
    }
}