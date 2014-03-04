using Abot.Poco;
using System;
using System.Threading;

namespace Abot.Crawler
{

    using Abot.Core;
    using log4net;
    using System.Diagnostics;
    using System.Reflection;

    public interface ICrawlerEngine : IHttpRequestEngine, IPageProcessorEngine
    {
        /// <summary>
        /// Registers a delegate to be called to determine whether a page should be crawled or not
        /// </summary>
        Func<PageToCrawl, CrawlContext, CrawlDecision> ShouldCrawlPageShortcut { get; set; }

        /// <summary>
        /// Begins a crawl using the uri param
        /// </summary>
        CrawlResult Crawl(Uri uri);

        /// <summary>
        /// Begins a crawl using the uri param, and can be cancelled using the CancellationToken
        /// </summary>
        CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource);

        /// <summary>
        /// Adds the uri to the current crawl
        /// </summary>
        /// <param name="uri"></param>
        void AddToCrawl(Uri uri);

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        dynamic CrawlBag { get; set; }

        /// <summary>
        /// Responsible for making http requests and publishing events
        /// </summary>
        IHttpRequestEngine HttpRequestEngine { get; set; }

        /// <summary>
        /// Responsible for processing the crawled page and publishing events
        /// </summary>
        IPageProcessorEngine CrawledPageProcessorEngine { get; set; }
    }

    public abstract class CrawlerEngine //: ICrawlerEngine
    {
        //!!!!!!!!!!!!!TODO make this a named logger before releasing 2.0!!!!!!!!!!!!!!
        static ILog _logger = LogManager.GetLogger(typeof(CrawlerEngine).FullName);
        //!!!!!!!!!!!!!TODO make these protected properties before releasing 2.0!!!!!!!!!!!!!!
        protected bool _crawlComplete = false;
        protected bool _crawlStopReported = false;
        protected bool _crawlCancellationReported = false;
        protected System.Timers.Timer _timeoutTimer;
        protected CrawlResult _crawlResult = null;
        protected CrawlContext _crawlContext;
        public IMemoryManager MemoryManager { get; set; }
        public ICrawlDecisionMaker CrawlDecisionMaker { get; set; }
        public IHttpRequestEngine HttpRequestEngine { get; set; }
        public IPageProcessorEngine CrawledPageProcessorEngine { get; set; }
        protected CancellationTokenSource HttpRequestEngineCancellationTokenSource { get; set; }
        protected CancellationTokenSource CrawledPageProcessorEngineCancellationTokenSource { get; set; }

        /// <summary>
        /// Dynamic object that can hold any value that needs to be available in the crawl context
        /// </summary>
        public dynamic CrawlBag { get; set; }

        #region Constructors

        static CrawlerEngine()
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

        public CrawlerEngine()
            : this(null)
        {
            
        }

        public CrawlerEngine(CrawlConfiguration crawlConfiguration)
            : this(crawlConfiguration, null, null, null, null)
        {
            
        }

        public CrawlerEngine(
            CrawlConfiguration crawlConfiguration, 
            ICrawlDecisionMaker crawlDecisionMaker,
            IHttpRequestEngine httpRequestEngine, 
            IPageProcessorEngine processorEngine,
            IMemoryManager memoryManager)
        {
            _crawlContext = new CrawlContext();
            _crawlContext.CrawlConfiguration = crawlConfiguration ?? GetCrawlConfigurationFromConfigFile();
            CrawlBag = _crawlContext.CrawlBag;

            HttpRequestEngine = httpRequestEngine ?? new HttpRequestEngine();
            CrawledPageProcessorEngine = processorEngine ?? new PageProcessorEngine();

            HttpRequestEngine.PageRequestCompleted += HttpRequestEngine_PageCrawlCompleted;
            //ProcessorEngine.PageProcessCompleted += FireEventHere;

            HttpRequestEngineCancellationTokenSource = new CancellationTokenSource();
            CrawledPageProcessorEngineCancellationTokenSource = new CancellationTokenSource();
            
            if (_crawlContext.CrawlConfiguration.MaxMemoryUsageInMb > 0
                || _crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb > 0)
                MemoryManager = memoryManager ?? new MemoryManager(new CachedMemoryMonitor(new GcMemoryMonitor(), _crawlContext.CrawlConfiguration.MaxMemoryUsageCacheTimeInSeconds));
        }

        private void HttpRequestEngine_PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            _crawlContext.PagesToProcess.Add(e.CrawledPage);
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

            _crawlContext.CancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();

            _crawlResult = new CrawlResult();
            _crawlResult.RootUri = _crawlContext.RootUri;
            _crawlResult.CrawlContext = _crawlContext;
            _crawlComplete = false;

            _logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);

            if (MemoryManager != null)
            {
                _crawlContext.MemoryUsageBeforeCrawlInMb = MemoryManager.GetCurrentUsageInMb();
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
                VerifyRequiredAvailableMemory();
                CrawlSite(uri);
            }
            catch (Exception e)
            {
                _crawlResult.ErrorException = e;
                _logger.FatalFormat("An error occurred while crawling site [{0}]", uri);
                _logger.Fatal(e);
            }

            if (_timeoutTimer != null)
                _timeoutTimer.Stop();

            timer.Stop();

            if (MemoryManager != null)
            {
                _crawlContext.MemoryUsageAfterCrawlInMb = MemoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, _crawlContext.MemoryUsageAfterCrawlInMb);
            }

            _crawlResult.Elapsed = timer.Elapsed;
            _logger.InfoFormat("Crawl complete for site [{0}]: [{1}]", _crawlResult.RootUri.AbsoluteUri, _crawlResult.Elapsed);

            return _crawlResult;
        }


        protected virtual void CrawlSite(Uri uri)
        {
            _crawlContext.PagesToCrawl.Add(new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true });
            
            //TODO add configuration for MaxConcurrentHttpRequests
            //TODO add configuration for MaxConcurrentCrawledPageProcessors
            //TODO retire MaxConcurrentThreads

            _logger.DebugFormat("Starting producer & consumer");
            HttpRequestEngine.Start(_crawlContext, null);//TODO pass real ShouldDownload or ShouldDownloadPageContentWrapper
            CrawledPageProcessorEngine.Start(_crawlContext, CrawledPageProcessorEngineCancellationTokenSource);

            while (!_crawlComplete)
            {
                RunPreWorkChecks();
                if (HttpRequestEngine.IsDone && CrawledPageProcessorEngine.IsDone)
                {
                    _crawlContext.PagesToCrawl.CompleteAdding();
                    _crawlContext.PagesToProcess.CompleteAdding();
                    HttpRequestEngine.Stop();
                    CrawledPageProcessorEngine.Stop();
                    _crawlComplete = true;
                }
                else
                {
                    _logger.DebugFormat("Health check, still working...");
                    System.Threading.Thread.Sleep(2500);                    
                }
            }

        }

        protected virtual void VerifyRequiredAvailableMemory()
        {
            if (_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb < 1)
                return;

            if (!MemoryManager.IsSpaceAvailable(_crawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
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
            if (MemoryManager == null
                || _crawlContext.IsCrawlHardStopRequested
                || _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb < 1)
                return;

            int currentMemoryUsage = MemoryManager.GetCurrentUsageInMb();
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("Current memory usage for site [{0}] is [{1}mb]", _crawlContext.RootUri, currentMemoryUsage);

            if (currentMemoryUsage > _crawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
            {
                MemoryManager.Dispose();
                MemoryManager = null;

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

                //Scheduler.Clear();
                //ThreadManager.AbortAll();
                //Scheduler.Clear();//to be sure nothing was scheduled since first call to clear()

                ////Set all events to null so no more events are fired
                //PageCrawlStarting = null;
                //PageCrawlCompleted = null;
                //PageCrawlDisallowed = null;
                ////PageLinksCrawlDisallowed = null;
                //PageCrawlStartingAsync = null;
                //PageCrawlCompletedAsync = null;
                //PageCrawlDisallowedAsync = null;
                //PageLinksCrawlDisallowedAsync = null;

                HttpRequestEngineCancellationTokenSource.Cancel();
                CrawledPageProcessorEngineCancellationTokenSource.Cancel();
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

                HttpRequestEngineCancellationTokenSource.Cancel();
                CrawledPageProcessorEngineCancellationTokenSource.Cancel();
                //TODO Not sure what to do here!!!!, how do we soft stop?????????
                //_scheduler.Clear();
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

        //protected virtual CrawlDecision ShouldDownloadPageContentWrapper(CrawledPage crawledPage)
        //{
        //    CrawlDecision decision = CrawlDecisionMaker.ShouldDownloadPageContent(crawledPage, _crawlContext);
        //    if (decision.Allow)
        //        decision = (_shouldDownloadPageContentDecisionMaker != null) ? _shouldDownloadPageContentDecisionMaker.Invoke(crawledPage, _crawlContext) : new CrawlDecision { Allow = true };

        //    SignalCrawlStopIfNeeded(decision);
        //    return decision;
        //}

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

        private CrawlConfiguration GetCrawlConfigurationFromConfigFile()
        {
            AbotConfigurationSectionHandler configFromFile = AbotConfigurationSectionHandler.LoadFromXml();

            if (configFromFile == null)
                throw new InvalidOperationException("abot config section was NOT found");

            _logger.DebugFormat("abot config section was found");
            return configFromFile.Convert();
        }
    }
}
