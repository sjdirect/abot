using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Abot2.Core;
using Abot2.Poco;
using Abot2.Util;
using Timer = System.Timers.Timer;
using Serilog;
using Serilog.Events;

namespace Abot2.Crawler
{
    public interface IWebCrawler : IDisposable
    {
        /// <summary>
        /// Event that is fired before a page is crawled.
        /// </summary>
        event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

        /// <summary>
        /// Event that is fired when an individual page has been crawled.
        /// </summary>
        event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

        /// <summary>
        /// Event that is fired when the ICrawlDecisionMaker.ShouldCrawl impl returned false. This means the page or its links were not crawled.
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <summary>
        /// Event that is fired when the ICrawlDecisionMaker.ShouldCrawlLinks impl returned false. This means the page's links were not crawled.
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;


        /// <summary>
        /// Delegate to be called to determine whether a page should be crawled or not
        /// </summary>
        Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageDecisionMaker { get; set; }

        /// <summary>
        /// Delegate to be called to determine whether the page's content should be downloaded
        /// </summary>
        Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContentDecisionMaker { get; set;}

        /// <summary>
        /// Delegate to be called to determine whether a page's links should be crawled or not
        /// </summary>
        Func<CrawledPage, CrawlContext, CrawlDecision> ShouldCrawlPageLinksDecisionMaker { get; set; }

        /// <summary>
        /// Delegate to be called to determine whether a certain link on a page should be scheduled to be crawled
        /// </summary>
        Func<Uri, CrawledPage, CrawlContext, bool> ShouldScheduleLinkDecisionMaker { get; set; }

        /// <summary>
        /// Delegate to be called to determine whether a page should be recrawled
        /// </summary>
        Func<CrawledPage, CrawlContext, CrawlDecision> ShouldRecrawlPageDecisionMaker { get; set; }

        /// <summary>
        /// Delegate to be called to determine whether the 1st uri param is considered an internal uri to the second uri param
        /// </summary>
        Func<Uri, Uri, bool> IsInternalUriDecisionMaker { get; set; }


        /// <summary>
        /// Begins a crawl using the uri param
        /// </summary>
        Task<CrawlResult> CrawlAsync(Uri uri);

        /// <summary>
        /// Begins a crawl using the uri param, and can be cancelled using the CancellationToken
        /// </summary>
        Task<CrawlResult> CrawlAsync(Uri uri, CancellationTokenSource tokenSource);

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        dynamic CrawlBag { get; set; }
    }

    public abstract class WebCrawler : IWebCrawler
    {
        protected bool _crawlComplete = false;
        protected bool _crawlStopReported = false;
        protected bool _crawlCancellationReported = false;
        protected bool _maxPagesToCrawlLimitReachedOrScheduled = false;
        protected Timer _timeoutTimer;
        protected CrawlResult _crawlResult = null;
        protected CrawlContext _crawlContext;
        protected IThreadManager _threadManager;
        protected IScheduler _scheduler;
        protected IPageRequester _pageRequester;
        protected IHtmlParser _htmlParser;
        protected ICrawlDecisionMaker _crawlDecisionMaker;
        protected IMemoryManager _memoryManager;
        protected int _processingPageCount = 0;
        private readonly object _processingPageCountLock = new object();

        #region Public properties

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        public dynamic CrawlBag { get; set; }

        /// <inheritdoc />
        public Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageDecisionMaker { get; set; }

        /// <inheritdoc />
        public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldDownloadPageContentDecisionMaker { get; set; }

        /// <inheritdoc />
        public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldCrawlPageLinksDecisionMaker { get; set; }

        /// <inheritdoc />
        public Func<CrawledPage, CrawlContext, CrawlDecision> ShouldRecrawlPageDecisionMaker { get; set; }

        /// <inheritdoc />
        public Func<Uri, CrawledPage, CrawlContext, bool> ShouldScheduleLinkDecisionMaker { get; set; }

        /// <inheritdoc />
        public Func<Uri, Uri, bool> IsInternalUriDecisionMaker { get; set; } = (uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a crawler instance with the default settings and implementations.
        /// </summary>
        public WebCrawler()
            : this(new CrawlConfiguration(), null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a crawler instance with custom settings or implementation. Passing in null for all params is the equivalent of the empty constructor.
        /// </summary>
        /// <param name="threadManager">Distributes http requests over multiple threads</param>
        /// <param name="scheduler">Decides what link should be crawled next</param>
        /// <param name="pageRequester">Makes the raw http requests</param>
        /// <param name="htmlParser">Parses a crawled page for it's hyperlinks</param>
        /// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
        /// <param name="crawlConfiguration">Configurable crawl values</param>
        /// <param name="memoryManager">Checks the memory usage of the host process</param>
        public WebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHtmlParser htmlParser,
            IMemoryManager memoryManager)
        {
            _crawlContext = new CrawlContext
            {
                CrawlConfiguration = crawlConfiguration ?? new CrawlConfiguration()
            };
            CrawlBag = _crawlContext.CrawlBag;

            _threadManager = threadManager ?? new TaskThreadManager(_crawlContext.CrawlConfiguration.MaxConcurrentThreads > 0 ? _crawlContext.CrawlConfiguration.MaxConcurrentThreads : Environment.ProcessorCount);
            _scheduler = scheduler ?? new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, null, null);
            _pageRequester = pageRequester ?? new PageRequester(_crawlContext.CrawlConfiguration, new WebContentExtractor());
            _crawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();

            if (_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb > 0
                || _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb > 0)
                _memoryManager = memoryManager ?? new MemoryManager(new CachedMemoryMonitor(new GcMemoryMonitor(), _crawlContext.CrawlConfiguration.MaxMemoryUsageCacheTimeInSeconds));

            _htmlParser = htmlParser ?? new AngleSharpHyperlinkParser(_crawlContext.CrawlConfiguration, null);

            _crawlContext.Scheduler = _scheduler;
        }

        #endregion Constructors

        #region Public Methods

        /// <inheritdoc />
        public virtual Task<CrawlResult> CrawlAsync(Uri uri) => CrawlAsync(uri, null);

        /// <inheritdoc />
        public virtual async Task<CrawlResult> CrawlAsync(Uri uri, CancellationTokenSource cancellationTokenSource)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            _crawlContext.RootUri = _crawlContext.OriginalRootUri = uri;

            if (cancellationTokenSource != null)
                _crawlContext.CancellationTokenSource = cancellationTokenSource;

            _crawlResult = new CrawlResult();
            _crawlResult.RootUri = _crawlContext.RootUri;
            _crawlResult.CrawlContext = _crawlContext;
            _crawlComplete = false;

            Log.Information("About to crawl site [{0}]", uri.AbsoluteUri);
            PrintConfigValues(_crawlContext.CrawlConfiguration);

            if (_memoryManager != null)
            {
                _crawlContext.MemoryUsageBeforeCrawlInMb = _memoryManager.GetCurrentUsageInMb();
                Log.Information("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageBeforeCrawlInMb);
            }

            _crawlContext.CrawlStartDate = DateTime.Now;
            var timer = Stopwatch.StartNew();

            if (_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds > 0)
            {
                _timeoutTimer = new Timer(_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds * 1000);
                _timeoutTimer.Elapsed += HandleCrawlTimeout;
                _timeoutTimer.Start();
            }

            try
            {
                var rootPage = new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true };
                if (ShouldSchedulePageLink(rootPage))
                    _scheduler.Add(rootPage);

                VerifyRequiredAvailableMemory();
                await CrawlSite();
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                Log.Fatal("An error occurred while crawling site [{0}]", uri);
                Log.Fatal(e, "Exception details -->");
            }
            finally
            {
                _threadManager?.Dispose();
            }

            _timeoutTimer?.Stop();

            timer.Stop();

            if (_memoryManager != null)
            {
                _crawlContext.MemoryUsageAfterCrawlInMb = _memoryManager.GetCurrentUsageInMb();
                Log.Information("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageAfterCrawlInMb);
            }

            _crawlResult.Elapsed = timer.Elapsed;
            Log.Information("Crawl complete for site [{0}]: Crawled [{1}] pages in [{2}]", _crawlResult.RootUri.AbsoluteUri, _crawlResult.CrawlContext.CrawledCount, _crawlResult.Elapsed);

            return _crawlResult;
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            _threadManager?.Dispose();
            _scheduler?.Dispose();
            _pageRequester?.Dispose();
            _memoryManager?.Dispose();
        }

        #endregion

        #region Events

        /// <inheritdoc />
        public event EventHandler<PageCrawlStartingArgs> PageCrawlStarting;

        /// <inheritdoc />
        public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompleted;

        /// <inheritdoc />
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowed;

        /// <inheritdoc />
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowed;

        protected virtual void FirePageCrawlStartingEvent(PageToCrawl pageToCrawl)
        {
            try
            {
                var threadSafeEvent = PageCrawlStarting;
                threadSafeEvent?.Invoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl));
            }
            catch (Exception e)
            {
                Log.Error("An unhandled exception was thrown by a subscriber of the PageCrawlStarting event for url:" + pageToCrawl.Uri.AbsoluteUri);
                Log.Error(e, "Exception details -->");
            }
        }

        protected virtual void FirePageCrawlCompletedEvent(CrawledPage crawledPage)
        {
            try
            {
                var threadSafeEvent = PageCrawlCompleted;
                threadSafeEvent?.Invoke(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
            }
            catch (Exception e)
            {
                Log.Error("An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
                Log.Error(e, "Exception details -->");
            }
        }

        protected virtual void FirePageCrawlDisallowedEvent(PageToCrawl pageToCrawl, string reason)
        {
            try
            {
                var threadSafeEvent = PageCrawlDisallowed;
                threadSafeEvent?.Invoke(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason));
            }
            catch (Exception e)
            {
                Log.Error("An unhandled exception was thrown by a subscriber of the PageCrawlDisallowed event for url:" + pageToCrawl.Uri.AbsoluteUri);
                Log.Error(e, "Exception details -->");
            }
        }

        protected virtual void FirePageLinksCrawlDisallowedEvent(CrawledPage crawledPage, string reason)
        {
            try
            {
                var threadSafeEvent = PageLinksCrawlDisallowed;
                threadSafeEvent?.Invoke(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason));
            }
            catch (Exception e)
            {
                Log.Error("An unhandled exception was thrown by a subscriber of the PageLinksCrawlDisallowed event for url:" + crawledPage.Uri.AbsoluteUri);
                Log.Error(e, "Exception details -->");
            }
        }

        #endregion

        #region Procected Async Methods

        protected virtual async Task CrawlSite()
        {
            while (!_crawlComplete)
            {
                RunPreWorkChecks();

                var linksToScheduleCount = _scheduler.Count;
                if (linksToScheduleCount > 0)
                {
                    Log.Debug($"There are [{linksToScheduleCount}] links to schedule...");
                    _threadManager.DoWork(async () => await ProcessPage(_scheduler.GetNext()));
                }
                else if (!_threadManager.HasRunningThreads() && _processingPageCount < 1)//Ok that _processingPageCount could be a race condition, will be caught on the next loop iteration
                {
                    Log.Debug("No links to schedule, no threads/tasks in progress...");
                    _crawlComplete = true;
                }
                else
                {
                    Log.Debug("Waiting for links to be scheduled...");

                    //Beware of issues here... https://github.com/sjdirect/abot/issues/203
                    await Task.Delay(2500).ConfigureAwait(false);
                }
            }
        }

        protected virtual async Task ProcessPage(PageToCrawl pageToCrawl)
        {
            lock (_processingPageCountLock)
            {
                _processingPageCount++;
                Log.Debug($"Incrementing processingPageCount to [{_processingPageCount}]");
            }

            try
            {
                if (pageToCrawl == null)
                    return;

                ThrowIfCancellationRequested();

                AddPageToContext(pageToCrawl);

                var crawledPage = await CrawlThePage(pageToCrawl).ConfigureAwait(false);

                // Validate the root uri in case of a redirection.
                if (crawledPage.IsRoot)
                    ValidateRootUriForRedirection(crawledPage);

                if (IsRedirect(crawledPage) && !_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
                    ProcessRedirect(crawledPage);

                if (PageSizeIsAboveMax(crawledPage))
                    return;

                ThrowIfCancellationRequested();

                var shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
                if (shouldCrawlPageLinks || _crawlContext.CrawlConfiguration.IsForcedLinkParsingEnabled)
                    ParsePageLinks(crawledPage);

                ThrowIfCancellationRequested();

                if (shouldCrawlPageLinks)
                    SchedulePageLinks(crawledPage);

                ThrowIfCancellationRequested();

                FirePageCrawlCompletedEvent(crawledPage);

                if (ShouldRecrawlPage(crawledPage))
                {
                    crawledPage.IsRetry = true;
                    _scheduler.Add(crawledPage);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Thread cancelled while crawling/processing page [{0}]", pageToCrawl.Uri);
                throw;
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                Log.Fatal("Error occurred during processing of page [{0}]", pageToCrawl.Uri);
                Log.Fatal(e, "Exception details -->");

                _crawlContext.IsCrawlHardStopRequested = true;
            }
            finally
            {
                lock (_processingPageCountLock)
                {
                    _processingPageCount--;
                    Log.Debug($"Decrementing processingPageCount to [{_processingPageCount}]");
                }
            }
        }

        protected virtual async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl)
        {
            Log.Debug("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);
            FirePageCrawlStartingEvent(pageToCrawl);

            if (pageToCrawl.IsRetry) { WaitMinimumRetryDelay(pageToCrawl); }

            pageToCrawl.LastRequest = DateTime.Now;

            var crawledPage = await _pageRequester.MakeRequestAsync(pageToCrawl.Uri, ShouldDownloadPageContent).ConfigureAwait(false);

            Map(pageToCrawl, crawledPage);

            if (crawledPage.HttpResponseMessage == null)
                Log.Information("Page crawl complete, Status:[NA] Url:[{0}] Elapsed:[{1}] Parent:[{2}] Retry:[{3}]", crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.ParentUri, crawledPage.RetryCount);
            else
                Log.Information("Page crawl complete, Status:[{0}] Url:[{1}] Elapsed:[{2}] Parent:[{3}] Retry:[{4}]", Convert.ToInt32(crawledPage.HttpResponseMessage.StatusCode), crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.ParentUri, crawledPage.RetryCount);

            return crawledPage;
        }

        #endregion

        #region Procted Methods

        protected virtual void VerifyRequiredAvailableMemory()
        {
            if (_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb < 1)
                return;

            if (!_memoryManager.IsSpaceAvailable(_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
                throw new InsufficientMemoryException($"Process does not have the configured [{_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb}mb] of available memory to crawl site [{_crawlContext.RootUri}]. This is configurable through the minAvailableMemoryRequiredInMb in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.");
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

            var currentMemoryUsage = _memoryManager.GetCurrentUsageInMb();
            if (Log.IsEnabled(LogEventLevel.Debug))
                Log.Debug("Current memory usage for site [{0}] is [{1}mb]", _crawlContext.RootUri, currentMemoryUsage);

            if (currentMemoryUsage > _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
            {
                _memoryManager.Dispose();
                _memoryManager = null;

                var message = string.Format("Process is using [{0}mb] of memory which is above the max configured of [{1}mb] for site [{2}]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", currentMemoryUsage, _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb, _crawlContext.RootUri);
                _crawlResult.ErrorException = new InsufficientMemoryException(message);

                Log.Fatal(_crawlResult.ErrorException, "Exception details -->");
                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }

        protected virtual void CheckForCancellationRequest()
        {
            if (_crawlContext.CancellationTokenSource.IsCancellationRequested)
            {
                if (!_crawlCancellationReported)
                {
                    var message = string.Format("Crawl cancellation requested for site [{0}]!", _crawlContext.RootUri);
                    Log.Fatal(message);
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
                    Log.Information("Hard crawl stop requested for site [{0}]!", _crawlContext.RootUri);
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
            }
        }

        protected virtual void CheckForStopRequest()
        {
            if (_crawlContext.IsCrawlStopRequested)
            {
                if (!_crawlStopReported)
                {
                    Log.Information("Crawl stop requested for site [{0}]!", _crawlContext.RootUri);
                    _crawlStopReported = true;
                }
                _scheduler.Clear();
            }
        }

        protected virtual void HandleCrawlTimeout(object sender, ElapsedEventArgs e)
        {
            var elapsedTimer = sender as Timer;
            elapsedTimer?.Stop();

            Log.Information("Crawl timeout of [{0}] seconds has been reached for [{1}]", _crawlContext.CrawlConfiguration.CrawlTimeoutSeconds, _crawlContext.RootUri);
            _crawlContext.IsCrawlHardStopRequested = true;
        }

        protected virtual void ProcessRedirect(CrawledPage crawledPage)
        {
            if (crawledPage.RedirectPosition >= 20)
                Log.Warning("Page [{0}] is part of a chain of 20 or more consecutive redirects, redirects for this chain will now be aborted.", crawledPage.Uri);

            try
            {
                var uri = ExtractRedirectUri(crawledPage);

                var page = new PageToCrawl(uri);
                page.ParentUri = crawledPage.ParentUri;
                page.CrawlDepth = crawledPage.CrawlDepth;
                page.IsInternal = IsInternalUri(uri);
                page.IsRoot = false;
                page.RedirectedFrom = crawledPage;
                page.RedirectPosition = crawledPage.RedirectPosition + 1;

                crawledPage.RedirectedTo = page;
                Log.Debug("Page [{0}] is requesting that it be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);

                if (ShouldSchedulePageLink(page))
                {
                    Log.Information("Page [{0}] will be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);
                    _scheduler.Add(page);
                }
            }
            catch {}
        }

        protected virtual bool IsInternalUri(Uri uri)
        {
            return  IsInternalUriDecisionMaker(uri, _crawlContext.RootUri) ||
                IsInternalUriDecisionMaker(uri, _crawlContext.OriginalRootUri);
        }

        protected virtual bool IsRedirect(CrawledPage crawledPage)
        {
            var isRedirect = false;
            if (crawledPage.HttpResponseMessage != null)
            {
                isRedirect = (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
                    crawledPage.HttpResponseMessage.RequestMessage.RequestUri != null &&
                    crawledPage.HttpResponseMessage.RequestMessage.RequestUri.AbsoluteUri != crawledPage.Uri.AbsoluteUri) ||
                    (!_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled &&
                    (int)crawledPage.HttpResponseMessage.StatusCode >= 300 &&
                    (int)crawledPage.HttpResponseMessage.StatusCode <= 399);
            }
            return isRedirect;
        }

        protected virtual void ThrowIfCancellationRequested()
        {
            if (_crawlContext.CancellationTokenSource != null && _crawlContext.CancellationTokenSource.IsCancellationRequested)
                _crawlContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        protected virtual bool PageSizeIsAboveMax(CrawledPage crawledPage)
        {
            var isAboveMax = false;
            if (_crawlContext.CrawlConfiguration.MaxPageSizeInBytes > 0 &&
                crawledPage.Content.Bytes != null &&
                crawledPage.Content.Bytes.Length > _crawlContext.CrawlConfiguration.MaxPageSizeInBytes)
            {
                isAboveMax = true;
                Log.Information("Page [{0}] has a page size of [{1}] bytes which is above the [{2}] byte max, no further processing will occur for this page", crawledPage.Uri, crawledPage.Content.Bytes.Length, _crawlContext.CrawlConfiguration.MaxPageSizeInBytes);
            }
            return isAboveMax;
        }

        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            var shouldCrawlPageLinksDecision = _crawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, _crawlContext);
            if (shouldCrawlPageLinksDecision.Allow)
                shouldCrawlPageLinksDecision = (ShouldCrawlPageLinksDecisionMaker != null) ? ShouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldCrawlPageLinksDecision.Allow)
            {
                Log.Debug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);
                FirePageLinksCrawlDisallowedEvent(crawledPage, shouldCrawlPageLinksDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageLinksDecision);
            return shouldCrawlPageLinksDecision.Allow;
        }

        protected virtual bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            if (_maxPagesToCrawlLimitReachedOrScheduled)
                return false;

            var shouldCrawlPageDecision = _crawlDecisionMaker.ShouldCrawlPage(pageToCrawl, _crawlContext);
            if (!shouldCrawlPageDecision.Allow &&
                shouldCrawlPageDecision.Reason.Contains("MaxPagesToCrawl limit of"))
            {
                _maxPagesToCrawlLimitReachedOrScheduled = true;
                Log.Information("MaxPagesToCrawlLimit has been reached or scheduled. No more pages will be scheduled.");
                return false;
            }

            if (shouldCrawlPageDecision.Allow)
                shouldCrawlPageDecision = (ShouldCrawlPageDecisionMaker != null) ? ShouldCrawlPageDecisionMaker(pageToCrawl, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldCrawlPageDecision.Allow)
            {
                Log.Debug("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
            }

            SignalCrawlStopIfNeeded(shouldCrawlPageDecision);
            return shouldCrawlPageDecision.Allow;
        }

        protected virtual bool ShouldRecrawlPage(CrawledPage crawledPage)
        {
            //TODO No unit tests cover these lines
            var shouldRecrawlPageDecision = _crawlDecisionMaker.ShouldRecrawlPage(crawledPage, _crawlContext);
            if (shouldRecrawlPageDecision.Allow)
                shouldRecrawlPageDecision = (ShouldRecrawlPageDecisionMaker != null) ? ShouldRecrawlPageDecisionMaker(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            if (!shouldRecrawlPageDecision.Allow)
            {
                Log.Debug("Page [{0}] not recrawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldRecrawlPageDecision.Reason);
            }
            else
            {
                // Look for the Retry-After header in the response.
                crawledPage.RetryAfter = null;

                var value = crawledPage.HttpResponseMessage?.Headers?.RetryAfter?.ToString();
                if (!String.IsNullOrEmpty(value))
                {
                    // Try to convert to DateTime first, then in double.
                    DateTime date;
                    double seconds;
                    if (crawledPage.LastRequest.HasValue && DateTime.TryParse(value, out date))
                    {
                        crawledPage.RetryAfter = (date - crawledPage.LastRequest.Value).TotalSeconds;
                    }
                    else if (double.TryParse(value, out seconds))
                    {
                        crawledPage.RetryAfter = seconds;
                    }
                }
            }

            SignalCrawlStopIfNeeded(shouldRecrawlPageDecision);
            return shouldRecrawlPageDecision.Allow;
        }

        protected virtual void Map(PageToCrawl src, CrawledPage dest)
        {
            dest.Uri = src.Uri;
            dest.ParentUri = src.ParentUri;
            dest.IsRetry = src.IsRetry;
            dest.RetryAfter = src.RetryAfter;
            dest.RetryCount = src.RetryCount;
            dest.LastRequest = src.LastRequest;
            dest.IsRoot = src.IsRoot;
            dest.IsInternal = src.IsInternal;
            dest.PageBag = CombinePageBags(src.PageBag, dest.PageBag);
            dest.CrawlDepth = src.CrawlDepth;
            dest.RedirectedFrom = src.RedirectedFrom;
            dest.RedirectPosition = src.RedirectPosition;
        }

        protected virtual dynamic CombinePageBags(dynamic pageToCrawlBag, dynamic crawledPageBag)
        {
            IDictionary<string, object> combinedBag = new ExpandoObject();
            var pageToCrawlBagDict = pageToCrawlBag as IDictionary<string, object>;
            var crawledPageBagDict = crawledPageBag as IDictionary<string, object>;

            foreach (var entry in pageToCrawlBagDict) combinedBag[entry.Key] = entry.Value;
            foreach (var entry in crawledPageBagDict) combinedBag[entry.Key] = entry.Value;

            return combinedBag;
        }

        protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
        {
            if (pageToCrawl.IsRetry)
            {
                pageToCrawl.RetryCount++;
                return;
            }

            Interlocked.Increment(ref _crawlContext.CrawledCount);
            _crawlContext.CrawlCountByDomain.AddOrUpdate(pageToCrawl.Uri.Authority, 1, (key, oldValue) => oldValue + 1);
        }

        protected virtual void ParsePageLinks(CrawledPage crawledPage)
        {
            crawledPage.ParsedLinks = _htmlParser.GetLinks(crawledPage);
        }

        protected virtual void SchedulePageLinks(CrawledPage crawledPage)
        {
            var linksToCrawl = 0;
            foreach (var hyperLink in crawledPage.ParsedLinks)
            {
                // First validate that the link was not already visited or added to the list of pages to visit, so we don't
                // make the same validation and fire the same events twice.
                if (!_scheduler.IsUriKnown(hyperLink.HrefValue) &&
                    (ShouldScheduleLinkDecisionMaker == null || ShouldScheduleLinkDecisionMaker.Invoke(hyperLink.HrefValue, crawledPage, _crawlContext)))
                {
                    try //Added due to a bug in the Uri class related to this (http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed)
                    {
                        var page = new PageToCrawl(hyperLink.HrefValue);
                        page.ParentUri = crawledPage.Uri;
                        page.CrawlDepth = crawledPage.CrawlDepth + 1;
                        page.IsInternal = IsInternalUri(hyperLink.HrefValue);
                        page.IsRoot = false;

                        if (ShouldSchedulePageLink(page))
                        {
                            _scheduler.Add(page);
                            linksToCrawl++;
                        }

                        if (!ShouldScheduleMorePageLink(linksToCrawl))
                        {
                            Log.Information("MaxLinksPerPage has been reached. No more links will be scheduled for current page [{0}].", crawledPage.Uri);
                            break;
                        }
                    }
                    catch { }
                }

                // Add this link to the list of known Urls so validations are not duplicated in the future.
                _scheduler.AddKnownUri(hyperLink.HrefValue);
            }
        }

        protected virtual bool ShouldSchedulePageLink(PageToCrawl page)
        {
            if ((page.IsInternal || _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled) && (ShouldCrawlPage(page)))
                return true;

            return false;
        }

        protected virtual bool ShouldScheduleMorePageLink(int linksAdded)
        {
            return _crawlContext.CrawlConfiguration.MaxLinksPerPage == 0 || _crawlContext.CrawlConfiguration.MaxLinksPerPage > linksAdded;
        }

        protected virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage)
        {
            var decision = _crawlDecisionMaker.ShouldDownloadPageContent(crawledPage, _crawlContext);
            if (decision.Allow)
                decision = (ShouldDownloadPageContentDecisionMaker != null) ? ShouldDownloadPageContentDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

            SignalCrawlStopIfNeeded(decision);
            return decision;
        }

        protected virtual void PrintConfigValues(CrawlConfiguration config)
        {
            Log.Information("Configuration Values:");

            var indentString = new string(' ', 2);
            var abotVersion = Assembly.GetAssembly(this.GetType()).GetName().Version.ToString();
            Log.Information("{0}Abot Version: {1}", indentString, abotVersion);
            foreach (var property in config.GetType().GetProperties())
            {
                if (property.Name != "ConfigurationExtensions")
                    Log.Information("{0}{1}: {2}", indentString, property.Name, property.GetValue(config, null));
            }

            foreach (var key in config.ConfigurationExtensions.Keys)
            {
                Log.Information("{0}{1}: {2}", indentString, key, config.ConfigurationExtensions[key]);
            }
        }

        protected virtual void SignalCrawlStopIfNeeded(CrawlDecision decision)
        {
            if (decision.ShouldHardStopCrawl)
            {
                Log.Information("Decision marked crawl [Hard Stop] for site [{0}], [{1}]", _crawlContext.RootUri, decision.Reason);
                _crawlContext.IsCrawlHardStopRequested = decision.ShouldHardStopCrawl;
            }
            else if (decision.ShouldStopCrawl)
            {
                Log.Information("Decision marked crawl [Stop] for site [{0}], [{1}]", _crawlContext.RootUri, decision.Reason);
                _crawlContext.IsCrawlStopRequested = decision.ShouldStopCrawl;
            }
        }

        protected virtual void WaitMinimumRetryDelay(PageToCrawl pageToCrawl)
        {
            //TODO No unit tests cover these lines
            if (pageToCrawl.LastRequest == null)
            {
                Log.Warning("pageToCrawl.LastRequest value is null for Url:{0}. Cannot retry without this value.", pageToCrawl.Uri.AbsoluteUri);
                return;
            }

            var milliSinceLastRequest = (DateTime.Now - pageToCrawl.LastRequest.Value).TotalMilliseconds;
            double milliToWait;
            if (pageToCrawl.RetryAfter.HasValue)
            {
                // Use the time to wait provided by the server instead of the config, if any.
                milliToWait = pageToCrawl.RetryAfter.Value * 1000 - milliSinceLastRequest;
            }
            else
            {
                if (!(milliSinceLastRequest < _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds)) return;
                milliToWait = _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds - milliSinceLastRequest;
            }

            Log.Information("Waiting [{0}] milliseconds before retrying Url:[{1}] LastRequest:[{2}] SoonestNextRequest:[{3}]",
                milliToWait,
                pageToCrawl.Uri.AbsoluteUri,
                pageToCrawl.LastRequest,
                pageToCrawl.LastRequest.Value.AddMilliseconds(_crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds));

            //TODO Cannot use RateLimiter since it currently cannot handle dynamic sleep times so using Thread.Sleep in the meantime
            if (milliToWait > 0)
                Thread.Sleep(TimeSpan.FromMilliseconds(milliToWait));
        }

        /// <summary>
        /// Validate that the Root page was not redirected. If the root page is redirected, we assume that the root uri
        /// should be changed to the uri where it was redirected.
        /// </summary>
        protected virtual void ValidateRootUriForRedirection(CrawledPage crawledRootPage)
        {
            if (!crawledRootPage.IsRoot)
            {
                throw new ArgumentException("The crawled page must be the root page to be validated for redirection.");
            }

            if (IsRedirect(crawledRootPage))
            {
                _crawlContext.RootUri = ExtractRedirectUri(crawledRootPage);
                Log.Information("The root URI [{0}] was redirected to [{1}]. [{1}] is the new root.",
                    _crawlContext.OriginalRootUri,
                    _crawlContext.RootUri);
            }
        }

        /// <summary>
        /// Retrieve the URI where the specified crawled page was redirected.
        /// </summary>
        /// <remarks>
        /// If HTTP auto redirections is disabled, this value is stored in the 'Location' header of the response.
        /// If auto redirections is enabled, this value is stored in the response's ResponseUri property.
        /// </remarks>
        protected virtual Uri ExtractRedirectUri(CrawledPage crawledPage)
        {
            Uri locationUri;
            if (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled)
            {
                // For auto redirects, look for the response uri.
                locationUri = crawledPage.HttpResponseMessage.RequestMessage.RequestUri;
            }
            else
            {
                // For manual redirects, we need to look for the location header.
                var location = crawledPage.HttpResponseMessage?.Headers?.Location?.AbsoluteUri;

                // Check if the location is absolute. If not, create an absolute uri.
                if (!Uri.TryCreate(location, UriKind.Absolute, out locationUri))
                {
                    var baseUri = new Uri(crawledPage.Uri.GetLeftPart(UriPartial.Authority));
                    locationUri = new Uri(baseUri, location);
                }
            }
            return locationUri;
        }

        #endregion
    }
}