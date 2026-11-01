using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        /// <param name="uri">The root of the site to crawl</param>
        /// <param name="cancellationToken">Cancels the crawl, including requests in progress. The result then reports an OperationCanceledException.</param>
        Task<CrawlResult> CrawlAsync(Uri uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a crawl using the uri param, and can be cancelled using the CancellationToken
        /// </summary>
        [Obsolete("Use CrawlAsync(Uri, CancellationToken) and pass the token of the source.")]
        Task<CrawlResult> CrawlAsync(Uri uri, CancellationTokenSource tokenSource);

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        dynamic CrawlBag { get; set; }
    }

    public abstract class WebCrawler : IWebCrawler
    {
        protected volatile bool _crawlComplete = false;
        protected bool _crawlStopReported = false;
        protected bool _crawlCancellationReported = false;
        protected volatile bool _maxPagesToCrawlLimitReachedOrScheduled = false;
        [Obsolete("No longer used: the crawl timeout is a cancellation token created from the crawler's TimeProvider.")]
        protected Timer _timeoutTimer;
        protected CrawlResult _crawlResult = null;
        protected CrawlContext _crawlContext;
        [Obsolete("No longer used: the crawler processes pages on its own async workers, limited by CrawlConfiguration.MaxConcurrentThreads. Holds the thread manager passed to the obsolete constructor, if any.")]
        protected IThreadManager _threadManager;
        protected IScheduler _scheduler;
        protected IPageRequester _pageRequester;
        protected IHtmlParser _htmlParser;
        protected ICrawlDecisionMaker _crawlDecisionMaker;
        protected IMemoryManager _memoryManager;

        /// <summary>
        /// Pages taken from the scheduler and not yet fully processed. Updated atomically.
        /// </summary>
        protected int _processingPageCount = 0;

        private readonly TimeProvider _timeProvider;
        private readonly int _maxConcurrentPages;

        //Pages counted against MaxPagesToCrawl: crawled, scheduled, or reserved by a page being scheduled right now. Reserved atomically,
        //so concurrent scheduling can never go over the limit, without a lock around the decision makers.
        private int _reservedPageCount;

        //Completed whenever there may be new work or the crawl state changed, then replaced. Workers wait on it instead of polling.
        private TaskCompletionSource<bool> _workChanged = NewWorkChangedSignal();

        //Cancelled by a hard stop or the crawl timeout, which also cancels requests in progress
        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Borrowed while a crawl runs; CrawlAsync owns it and disposes it in a using block")]
        private CancellationTokenSource _stopCrawl;

        private int _cancellationReported;
        private CancellationToken _callerCancellationToken;
        private int _stopReported;

        //What this instance created itself and therefore disposes. Anything passed in is owned by the caller.
        private readonly bool _ownsScheduler;
        private readonly bool _ownsPageRequester;
        private readonly bool _ownsMemoryManager;
        private readonly IWebContentExtractor _ownedContentExtractor;
        private readonly CachedMemoryMonitor _ownedCachedMemoryMonitor;
        private readonly GcMemoryMonitor _ownedGcMemoryMonitor;
        private int _isDisposed;

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
            : this(new CrawlConfiguration(), null, (IScheduler)null, null, null, null)
        {
        }

        /// <summary>
        /// Creates a crawler instance with custom settings or implementation. Passing in null for all params is the equivalent of the empty constructor.
        /// Implementations passed in are owned by the caller, who must dispose them after this crawler; the ones created for null params are disposed with this crawler.
        /// </summary>
        /// <param name="crawlConfiguration">Configurable crawl values</param>
        /// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
        /// <param name="scheduler">Decides what link should be crawled next</param>
        /// <param name="pageRequester">Makes the raw http requests</param>
        /// <param name="htmlParser">Parses a crawled page for it's hyperlinks</param>
        /// <param name="memoryManager">Checks the memory usage of the host process</param>
        /// <param name="timeProvider">Clock for the crawl: start time, request times, retry waits, the crawl timeout and the elapsed time; if null, the system clock (TimeProvider.System)</param>
        protected WebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHtmlParser htmlParser,
            IMemoryManager memoryManager,
            TimeProvider timeProvider = null)
            : this(crawlConfiguration, crawlDecisionMaker, null, scheduler, pageRequester, htmlParser, memoryManager, timeProvider)
        {
        }

        /// <summary>
        /// Creates a crawler instance with custom settings or implementation. Passing in null for all params is the equivalent of the empty constructor.
        /// Implementations passed in are owned by the caller, who must dispose them after this crawler; the ones created for null params are disposed with this crawler.
        /// </summary>
        /// <param name="threadManager">No longer used to run pages: the crawler processes pages on its own async workers. Its MaxThreads sets how many pages are processed at the same time.</param>
        /// <param name="scheduler">Decides what link should be crawled next</param>
        /// <param name="pageRequester">Makes the raw http requests</param>
        /// <param name="htmlParser">Parses a crawled page for it's hyperlinks</param>
        /// <param name="crawlDecisionMaker">Decides whether or not to crawl a page or that page's links</param>
        /// <param name="crawlConfiguration">Configurable crawl values</param>
        /// <param name="memoryManager">Checks the memory usage of the host process</param>
        [Obsolete("IThreadManager is no longer used to run pages. Use the constructor without it; CrawlConfiguration.MaxConcurrentThreads sets how many pages are processed at the same time.")]
        protected WebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            IThreadManager threadManager,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHtmlParser htmlParser,
            IMemoryManager memoryManager)
            : this(crawlConfiguration, crawlDecisionMaker, threadManager?.MaxThreads, scheduler, pageRequester, htmlParser, memoryManager, null)
        {
            _threadManager = threadManager;
        }

        private WebCrawler(
            CrawlConfiguration crawlConfiguration,
            ICrawlDecisionMaker crawlDecisionMaker,
            int? maxConcurrentPages,
            IScheduler scheduler,
            IPageRequester pageRequester,
            IHtmlParser htmlParser,
            IMemoryManager memoryManager,
            TimeProvider timeProvider)
        {
            _crawlContext = new CrawlContext
            {
                CrawlConfiguration = crawlConfiguration ?? new CrawlConfiguration()
            };
            CrawlBag = _crawlContext.CrawlBag;

            _timeProvider = timeProvider ?? TimeProvider.System;
            _maxConcurrentPages = maxConcurrentPages ?? (_crawlContext.CrawlConfiguration.MaxConcurrentThreads > 0 ? _crawlContext.CrawlConfiguration.MaxConcurrentThreads : Environment.ProcessorCount);

            _ownsScheduler = scheduler == null;
            _scheduler = scheduler ?? new Scheduler(_crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled, null, null);

            _ownsPageRequester = pageRequester == null;
            if (pageRequester == null)
            {
                _ownedContentExtractor = new WebContentExtractor();
                pageRequester = new PageRequester(_crawlContext.CrawlConfiguration, _ownedContentExtractor, null, _timeProvider);
            }
            _pageRequester = pageRequester;

            _crawlDecisionMaker = crawlDecisionMaker ?? new CrawlDecisionMaker();

            if (_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb > 0
                || _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb > 0)
            {
                _ownsMemoryManager = memoryManager == null;
                if (memoryManager == null)
                {
                    _ownedGcMemoryMonitor = new GcMemoryMonitor();
                    _ownedCachedMemoryMonitor = new CachedMemoryMonitor(_ownedGcMemoryMonitor, _crawlContext.CrawlConfiguration.MaxMemoryUsageCacheTimeInSeconds, _timeProvider);
                    memoryManager = new MemoryManager(_ownedCachedMemoryMonitor);
                }
                _memoryManager = memoryManager;
            }

            _htmlParser = htmlParser ?? new AngleSharpHyperlinkParser(_crawlContext.CrawlConfiguration, null);

            _crawlContext.Scheduler = _scheduler;
        }

        /// <summary>
        /// The clock the crawl uses
        /// </summary>
        protected TimeProvider TimeProvider => _timeProvider;

        #endregion Constructors

        #region Public Methods

        /// <inheritdoc />
        [Obsolete("Use CrawlAsync(Uri, CancellationToken) and pass the token of the source.")]
        public virtual Task<CrawlResult> CrawlAsync(Uri uri, CancellationTokenSource cancellationTokenSource)
        {
            return CrawlAsync(uri, cancellationTokenSource?.Token ?? CancellationToken.None);
        }

        /// <inheritdoc />
        public virtual async Task<CrawlResult> CrawlAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            ThrowIfDisposed();

            _crawlContext.RootUri = _crawlContext.OriginalRootUri = uri;

            _crawlResult = new CrawlResult();
            _crawlResult.RootUri = _crawlContext.RootUri;
            _crawlResult.CrawlContext = _crawlContext;
            _crawlComplete = false;
            _callerCancellationToken = cancellationToken;
            Volatile.Write(ref _workChanged, NewWorkChangedSignal());

            Log.Information("About to crawl site [{0}]", uri.AbsoluteUri);
            PrintConfigValues(_crawlContext.CrawlConfiguration);

            var memoryManager = _memoryManager;
            if (memoryManager != null)
            {
                _crawlContext.MemoryUsageBeforeCrawlInMb = memoryManager.GetCurrentUsageInMb();
                Log.Information("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageBeforeCrawlInMb);
            }

            _crawlContext.CrawlStartDate = Now();
            var startTimestamp = _timeProvider.GetTimestamp();

            //The crawl token: cancelled by the caller or by a handler through CrawlContext.CancellationTokenSource, and reported as a cancellation.
            //Work also stops on a hard stop or the crawl timeout, which are not errors.
            using (var crawlCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var stopCrawl = new CancellationTokenSource())
            using (var workCancellation = CancellationTokenSource.CreateLinkedTokenSource(crawlCancellation.Token, stopCrawl.Token))
            {
                _crawlContext.CancellationTokenSource = crawlCancellation;
                _stopCrawl = stopCrawl;
                Interlocked.Exchange(ref _cancellationReported, 0);

                CancellationTokenSource crawlTimeout = null;
                var timeoutRegistration = default(CancellationTokenRegistration);
                var cancellationRegistration = crawlCancellation.Token.Register(SignalWorkChanged);
                try
                {
                    if (_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds > 0)
                    {
                        crawlTimeout = TimeProviderWaits.CreateCancellationTokenSource(_timeProvider, TimeSpan.FromSeconds(_crawlContext.CrawlConfiguration.CrawlTimeoutSeconds));
                        timeoutRegistration = crawlTimeout.Token.Register(OnCrawlTimeout);
                    }

                    _reservedPageCount = _crawlContext.CrawledCount + _scheduler.Count;

                    ScheduleRootPage(new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true });

                    VerifyRequiredAvailableMemory();
                    await CrawlSite(workCancellation.Token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _crawlResult.ErrorException = e;
                    Log.Fatal("An error occurred while crawling site [{0}]", uri);
                    Log.Fatal(e, "Exception details -->");
                }
                finally
                {
                    timeoutRegistration.Dispose();
                    cancellationRegistration.Dispose();
                    crawlTimeout?.Dispose();
                    _stopCrawl = null;
                }
            }

            memoryManager = _memoryManager;
            if (memoryManager != null)
            {
                _crawlContext.MemoryUsageAfterCrawlInMb = memoryManager.GetCurrentUsageInMb();
                Log.Information("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageAfterCrawlInMb);
            }

            _crawlResult.Elapsed = _timeProvider.GetElapsedTime(startTimestamp);
            Log.Information("Crawl complete for site [{0}]: Crawled [{1}] pages in [{2}]", _crawlResult.RootUri.AbsoluteUri, _crawlResult.CrawlContext.CrawledCount, _crawlResult.Elapsed);

            return _crawlResult;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes what this instance created itself. Implementations passed to the constructor are owned by the caller and are not disposed.
        /// Derived classes override this to dispose what they own and then call base.Dispose(disposing).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (!disposing)
                return;

            //Stop a crawl that is still running before disposing what it uses
            _crawlContext.IsCrawlHardStopRequested = true;
            CancelStopCrawl();
            SignalWorkChanged();

            if (_ownsScheduler)
                _scheduler?.Dispose();

            if (_ownsPageRequester)
                _pageRequester?.Dispose();
            _ownedContentExtractor?.Dispose();

            if (_ownsMemoryManager)
                _memoryManager?.Dispose();
            _ownedCachedMemoryMonitor?.Dispose();
            _ownedGcMemoryMonitor?.Dispose();
        }

        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
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

        /// <summary>
        /// Processes scheduled pages on async workers until nothing is scheduled or in progress, or the crawl is stopped.
        /// At most MaxConcurrentThreads pages are processed at the same time, each until it is completely done.
        /// </summary>
        /// <param name="cancellationToken">Cancelled when the crawl is cancelled, hard stopped or times out</param>
        protected virtual Task CrawlSite(CancellationToken cancellationToken)
        {
            var workers = new Task[_maxConcurrentPages];
            for (var i = 0; i < workers.Length; i++)
                workers[i] = Task.Run(() => RunWorkerAsync(cancellationToken));

            return Task.WhenAll(workers);
        }

        [Obsolete("Use CrawlSite(CancellationToken).")]
        protected virtual Task CrawlSite()
        {
            return CrawlSite(_crawlContext.CancellationTokenSource.Token);
        }

        private async Task RunWorkerAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                //Taken before looking for work, so a signal sent after the look is not missed
                var workChanged = Volatile.Read(ref _workChanged).Task;

                RunPreWorkChecks();

                if (_crawlComplete)
                    return;

                var pageToCrawl = TakeNextPageToCrawl();
                if (pageToCrawl != null)
                {
                    await ProcessTakenPageAsync(pageToCrawl, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                //Read the in-progress count first: a page only stops counting after it has scheduled its links
                if (Volatile.Read(ref _processingPageCount) == 0 && (IsStopping || _scheduler.Count == 0))
                {
                    _crawlComplete = true;
                    SignalWorkChanged();
                    return;
                }

                await workChanged.ConfigureAwait(false);
            }
        }

        //Counts the page as in progress before taking it, so the crawl cannot be seen as complete while a page is being taken
        private PageToCrawl TakeNextPageToCrawl()
        {
            if (IsStopping)
                return null;

            Interlocked.Increment(ref _processingPageCount);
            PageToCrawl pageToCrawl = null;
            try
            {
                pageToCrawl = _scheduler.GetNext();
                if (pageToCrawl != null)
                    AddPageToContext(pageToCrawl);
                return pageToCrawl;
            }
            catch (Exception e)
            {
                //A custom scheduler or AddPageToContext override that throws stops the crawl like any processing error
                pageToCrawl = null;
                ReportCrawlError(e, "Error occurred while getting the next page for site [{0}]");
                return null;
            }
            finally
            {
                if (pageToCrawl == null)
                {
                    Interlocked.Decrement(ref _processingPageCount);
                    SignalWorkChanged();
                }
            }
        }

        private async Task ProcessTakenPageAsync(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
        {
            try
            {
                await ProcessPage(pageToCrawl, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                //A cancellation is reported on the crawl result by CheckForCancellationRequest(); a hard stop or timeout is not an error
            }
            catch (Exception e)
            {
                //ProcessPage handles its own errors, this catches ProcessPage() overrides
                ReportCrawlError(e, "Error occurred while processing a page for site [{0}]");
            }
            finally
            {
                Interlocked.Decrement(ref _processingPageCount);
                SignalWorkChanged();
            }
        }

        private void ReportCrawlError(Exception e, string message)
        {
            _crawlResult.ErrorException = e;
            Log.Fatal(message, _crawlContext.RootUri);
            Log.Fatal(e, "Exception details -->");

            _crawlContext.IsCrawlHardStopRequested = true;
        }

        private bool IsStopping => _crawlContext.IsCrawlStopRequested || _crawlContext.IsCrawlHardStopRequested;

        /// <summary>
        /// Wakes the workers waiting for work, so they look again
        /// </summary>
        protected void SignalWorkChanged()
        {
            Interlocked.Exchange(ref _workChanged, NewWorkChangedSignal()).TrySetResult(true);
        }

        private static TaskCompletionSource<bool> NewWorkChangedSignal()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private void OnCrawlTimeout()
        {
            //Runs on a timer thread, where an exception escaping would crash the process
            try
            {
                HandleCrawlTimeout(null, null);
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception was thrown while handling the crawl timeout");
            }

            SignalWorkChanged();
        }

        private void CancelCrawl()
        {
            try
            {
                _crawlContext.CancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                //The crawl has already ended
            }
        }

        private void CancelStopCrawl()
        {
            try
            {
                Volatile.Read(ref _stopCrawl)?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                //The crawl has already ended
            }
        }

        /// <param name="pageToCrawl">The page to crawl and process</param>
        /// <param name="cancellationToken">Cancelled when the crawl is cancelled, hard stopped or times out</param>
        protected virtual async Task ProcessPage(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
        {
            if (pageToCrawl == null)
                return;

            try
            {
                ThrowIfCancellationRequested();

                var crawledPage = await CrawlThePage(pageToCrawl, cancellationToken).ConfigureAwait(false);

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
                    SignalWorkChanged();
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
        }

        [Obsolete("Use ProcessPage(PageToCrawl, CancellationToken).")]
        protected virtual Task ProcessPage(PageToCrawl pageToCrawl)
        {
            return ProcessPage(pageToCrawl, _crawlContext.CancellationTokenSource.Token);
        }

        /// <param name="pageToCrawl">The page to request</param>
        /// <param name="cancellationToken">Cancelled when the crawl is cancelled, hard stopped or times out</param>
        protected virtual async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
        {
            Log.Debug("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);
            FirePageCrawlStartingEvent(pageToCrawl);

            await BeforeRequestAsync(pageToCrawl, cancellationToken).ConfigureAwait(false);

            if (pageToCrawl.IsRetry)
                await WaitMinimumRetryDelayAsync(pageToCrawl, cancellationToken).ConfigureAwait(false);

            pageToCrawl.LastRequest = Now();

            var crawledPage = await _pageRequester.MakeRequestAsync(pageToCrawl.Uri, ShouldDownloadPageContent, cancellationToken).ConfigureAwait(false);

            Map(pageToCrawl, crawledPage);

            if (crawledPage.HttpResponseMessage == null)
                Log.Information("Page crawl complete, Status:[NA] Url:[{0}] Elapsed:[{1}] Parent:[{2}] Retry:[{3}]", crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.ParentUri, crawledPage.RetryCount);
            else
                Log.Information("Page crawl complete, Status:[{0}] Url:[{1}] Elapsed:[{2}] Parent:[{3}] Retry:[{4}]", Convert.ToInt32(crawledPage.HttpResponseMessage.StatusCode), crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.ParentUri, crawledPage.RetryCount);

            return crawledPage;
        }

        [Obsolete("Use CrawlThePage(PageToCrawl, CancellationToken).")]
        protected virtual Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl)
        {
            return CrawlThePage(pageToCrawl, _crawlContext.CancellationTokenSource.Token);
        }

        /// <summary>
        /// Runs after the PageCrawlStarting event and before the page is requested. Does nothing here; PoliteWebCrawler rate limits in it.
        /// </summary>
        /// <param name="pageToCrawl">The page about to be requested</param>
        /// <param name="cancellationToken">Cancelled when the crawl is cancelled</param>
        protected virtual Task BeforeRequestAsync(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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
            //A worker that finds the field cleared also sees the hard stop, which is set before the field is cleared
            var memoryManager = Volatile.Read(ref _memoryManager);
            if (memoryManager == null
                || _crawlContext.IsCrawlHardStopRequested
                || _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb < 1)
                return;

            var currentMemoryUsage = memoryManager.GetCurrentUsageInMb();
            if (Log.IsEnabled(LogEventLevel.Debug))
                Log.Debug("Current memory usage for site [{0}] is [{1}mb]", _crawlContext.RootUri, currentMemoryUsage);

            if (currentMemoryUsage <= _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
                return;

            //Stop before claiming the report, so no worker can take another page in between
            _crawlContext.IsCrawlHardStopRequested = true;

            //Of several workers seeing the limit exceeded, only the one that clears the field reports it
            if (Interlocked.CompareExchange(ref _memoryManager, null, memoryManager) == memoryManager)
            {
                //Stop the memory monitor now rather than when the crawler is disposed, but only if this instance owns it
                if (_ownsMemoryManager)
                {
                    memoryManager.Dispose();
                    _ownedCachedMemoryMonitor?.Dispose();
                }

                var message = string.Format("Process is using [{0}mb] of memory which is above the max configured of [{1}mb] for site [{2}]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", currentMemoryUsage, _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb, _crawlContext.RootUri);
                _crawlResult.ErrorException = new InsufficientMemoryException(message);

                Log.Fatal(_crawlResult.ErrorException, "Exception details -->");
            }
        }

        protected virtual void CheckForCancellationRequest()
        {
            //The caller's token reports a cancellation before its callbacks have cancelled the linked crawl token,
            //so a crawl could otherwise finish its last page in between and end without seeing the cancellation
            if (_callerCancellationToken.IsCancellationRequested)
                CancelCrawl();

            if (_crawlContext.CancellationTokenSource.IsCancellationRequested)
            {
                //Stop before claiming the report, so no worker can take another page in between
                _crawlContext.IsCrawlHardStopRequested = true;

                if (Interlocked.Exchange(ref _cancellationReported, 1) == 0)
                {
                    var message = string.Format("Crawl cancellation requested for site [{0}]!", _crawlContext.RootUri);
                    Log.Fatal(message);
                    _crawlResult.ErrorException = new OperationCanceledException(message, _crawlContext.CancellationTokenSource.Token);
                    _crawlCancellationReported = true;
                }
            }
        }

        protected virtual void CheckForHardStopRequest()
        {
            if (_crawlContext.IsCrawlHardStopRequested)
            {
                if (Interlocked.Exchange(ref _stopReported, 1) == 0)
                {
                    Log.Information("Hard crawl stop requested for site [{0}]!", _crawlContext.RootUri);
                    _crawlStopReported = true;
                }

                //Cancels requests and waits in progress
                CancelStopCrawl();
                _scheduler.Clear();

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
                if (Interlocked.Exchange(ref _stopReported, 1) == 0)
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

            if (!HasRedirectTarget(crawledPage))
                return;

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

                if ((_scheduler.AddKnownUri(page.Uri) || _crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled) && ScheduleKnownPage(page))
                    Log.Information("Page [{0}] will be redirect to [{1}]", crawledPage.Uri, crawledPage.RedirectedTo.Uri);
            }
            catch (UriFormatException e)
            {
                //A malformed Location header is the server's problem, not a crawl error: skip the redirect
                Log.Warning(e, "Page [{0}] redirects to an invalid uri, the redirect is not followed", crawledPage.Uri);
            }
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
                // Look for the Retry-After header in the response: either a date or a number of seconds
                crawledPage.RetryAfter = null;

                var retryAfter = crawledPage.HttpResponseMessage?.Headers?.RetryAfter;
                if (retryAfter?.Date != null && crawledPage.LastRequest.HasValue)
                    crawledPage.RetryAfter = (retryAfter.Date.Value - new DateTimeOffset(crawledPage.LastRequest.Value)).TotalSeconds;
                else if (retryAfter?.Delta != null)
                    crawledPage.RetryAfter = retryAfter.Delta.Value.TotalSeconds;
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
            // The object casts keep this a static call: dynamic arguments would bind it at runtime, which isn't AOT/trim safe
            dest.PageBag = CombinePageBags((object)src.PageBag, (object)dest.PageBag);
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
                // Marking the link as known is atomic, so of several pages crawled at the same time that link to it, only the first evaluates it
                // and its events fire once. A link is known from here on whatever the decision, so it is never evaluated again.
                if (!_scheduler.AddKnownUri(hyperLink.HrefValue))
                    continue;

                if (ShouldScheduleLinkDecisionMaker == null || ShouldScheduleLinkDecisionMaker.Invoke(hyperLink.HrefValue, crawledPage, _crawlContext))
                {
                    try
                    {
                        var page = new PageToCrawl(hyperLink.HrefValue);
                        page.ParentUri = crawledPage.Uri;
                        page.CrawlDepth = crawledPage.CrawlDepth + 1;
                        page.IsInternal = IsInternalUri(hyperLink.HrefValue);
                        page.IsRoot = false;

                        if (ScheduleKnownPage(page))
                            linksToCrawl++;
                    }
                    catch (UriFormatException e)
                    {
                        //Some uris parse but throw on use, for example when their host name cannot be parsed
                        //(http://stackoverflow.com/questions/2814951/system-uriformatexception-invalid-uri-the-hostname-could-not-be-parsed)
                        Log.Warning(e, "Link [{0}] on page [{1}] is not a valid uri and is skipped", hyperLink.HrefValue, crawledPage.Uri);
                    }
                }

                if (!ShouldScheduleMorePageLink(linksToCrawl))
                {
                    Log.Information("MaxLinksPerPage has been reached. No more links will be scheduled for current page [{0}].", crawledPage.Uri);
                    break;
                }
            }
        }

        //The root is always evaluated, even when its uri is already known, and then scheduled unless it is a duplicate.
        //It is scheduled before any worker starts, so nothing runs concurrently with it.
        private void ScheduleRootPage(PageToCrawl rootPage)
        {
            var countsTowardsLimit = _crawlContext.CrawlConfiguration.MaxPagesToCrawl > 0;
            if (countsTowardsLimit && !TryReservePageSlot())
                return;

            var scheduled = false;
            try
            {
                if (ShouldSchedulePageLink(rootPage)
                    && (_scheduler.AddKnownUri(rootPage.Uri) || _crawlContext.CrawlConfiguration.IsUriRecrawlingEnabled))
                {
                    _scheduler.AddKnownPage(rootPage);
                    scheduled = true;
                }
            }
            finally
            {
                if (countsTowardsLimit && !scheduled)
                    Interlocked.Decrement(ref _reservedPageCount);
            }
        }

        /// <summary>
        /// Schedules a page whose uri was marked as known, if the decision makers allow it and MaxPagesToCrawl has room.
        /// The room is reserved atomically before the decision and released if the page is not scheduled.
        /// </summary>
        private bool ScheduleKnownPage(PageToCrawl page)
        {
            var countsTowardsLimit = !page.IsRetry && _crawlContext.CrawlConfiguration.MaxPagesToCrawl > 0;
            if (countsTowardsLimit && !TryReservePageSlot())
                return false;

            var scheduled = false;
            try
            {
                if (ShouldSchedulePageLink(page))
                {
                    _scheduler.AddKnownPage(page);
                    scheduled = true;
                    SignalWorkChanged();
                }
            }
            finally
            {
                if (countsTowardsLimit && !scheduled)
                    Interlocked.Decrement(ref _reservedPageCount);
            }

            return scheduled;
        }

        private bool TryReservePageSlot()
        {
            if (Interlocked.Increment(ref _reservedPageCount) <= _crawlContext.CrawlConfiguration.MaxPagesToCrawl)
                return true;

            Interlocked.Decrement(ref _reservedPageCount);
            if (!_maxPagesToCrawlLimitReachedOrScheduled)
            {
                _maxPagesToCrawlLimitReachedOrScheduled = true;
                Log.Information("MaxPagesToCrawlLimit has been reached or scheduled. No more pages will be scheduled.");
            }
            return false;
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

        /// <summary>
        /// Waits, without blocking a thread, until the page may be retried: until its Retry-After time if the server sent one, otherwise until
        /// MinRetryDelayInMilliseconds after its last request
        /// </summary>
        /// <param name="pageToCrawl">The page about to be retried</param>
        /// <param name="cancellationToken">Cancels the wait</param>
        protected virtual Task WaitMinimumRetryDelayAsync(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
        {
            if (pageToCrawl.LastRequest == null)
            {
                Log.Warning("pageToCrawl.LastRequest value is null for Url:{0}. Cannot retry without this value.", pageToCrawl.Uri.AbsoluteUri);
                return Task.CompletedTask;
            }

            var milliSinceLastRequest = (Now() - pageToCrawl.LastRequest.Value).TotalMilliseconds;
            double milliToWait;
            if (pageToCrawl.RetryAfter.HasValue)
            {
                // Use the time to wait provided by the server instead of the config, if any.
                milliToWait = pageToCrawl.RetryAfter.Value * 1000 - milliSinceLastRequest;
            }
            else
            {
                if (!(milliSinceLastRequest < _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds))
                    return Task.CompletedTask;
                milliToWait = _crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds - milliSinceLastRequest;
            }

            Log.Information("Waiting [{0}] milliseconds before retrying Url:[{1}] LastRequest:[{2}] SoonestNextRequest:[{3}]",
                milliToWait,
                pageToCrawl.Uri.AbsoluteUri,
                pageToCrawl.LastRequest,
                pageToCrawl.LastRequest.Value.AddMilliseconds(_crawlContext.CrawlConfiguration.MinRetryDelayInMilliseconds));

            return milliToWait > 0
                ? TimeProviderWaits.Delay(_timeProvider, TimeSpan.FromMilliseconds(milliToWait), cancellationToken)
                : Task.CompletedTask;
        }

        [Obsolete("Blocks the calling thread while it waits. Use WaitMinimumRetryDelayAsync.")]
        protected virtual void WaitMinimumRetryDelay(PageToCrawl pageToCrawl)
        {
            WaitMinimumRetryDelayAsync(pageToCrawl, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The current local time on the crawler's clock
        /// </summary>
        protected DateTime Now()
        {
            return DateTime.SpecifyKind(_timeProvider.GetLocalNow().DateTime, DateTimeKind.Local);
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

            if (IsRedirect(crawledRootPage) && HasRedirectTarget(crawledRootPage))
            {
                _crawlContext.RootUri = ExtractRedirectUri(crawledRootPage);
                Log.Information("The root URI [{0}] was redirected to [{1}]. [{1}] is the new root.",
                    _crawlContext.OriginalRootUri,
                    _crawlContext.RootUri);
            }
        }

        //Without a Location header a manual redirect has nothing to follow; resolving the missing location would give the site's base uri
        private bool HasRedirectTarget(CrawledPage crawledPage)
        {
            if (_crawlContext.CrawlConfiguration.IsHttpRequestAutoRedirectsEnabled || crawledPage.HttpResponseMessage?.Headers?.Location != null)
                return true;

            Log.Warning("Page [{0}] answered with a redirect but has no Location header, there is nothing to follow", crawledPage.Uri);
            return false;
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
                var location = crawledPage.HttpResponseMessage?.Headers?.Location?.ToString();

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