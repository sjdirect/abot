using Abot.Poco;
using log4net;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Abot.Core
{
    public interface IWebCrawler
    {
        CrawlContext CrawlContext { get; set; }

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

        /// <summary>
        /// Responsible for making http requests and publishing related events
        /// </summary>
        IPageRequesterEngine PageRequesterEngine { get; set; }

        /// <summary>
        /// Responsible for processing the crawled page and publishing related events
        /// </summary>
        IPageProcessorEngine PageProcessorEngine { get; set; }
    }

    public class Crawler : IWebCrawler
    {
        //TODO Its this classes job to make using abot just as easy as the old one but also to
        //add access to things like the scheduler

        //!!!!!!!!!!!!!TODO make this a named logger before releasing 2.0!!!!!!!!!!!!!!
        static ILog _logger = LogManager.GetLogger(typeof(Crawler).FullName);

        protected bool _crawlStopReported = false;
        protected bool _crawlCancellationReported = false;
        protected System.Timers.Timer _timeoutTimer;
        protected CrawlResult _crawlResult = null;

        public CrawlContext CrawlContext { get; set; }
        public IPageRequesterEngine PageRequesterEngine { get; set; }
        public IPageProcessorEngine PageProcessorEngine { get; set; }
        public dynamic CrawlBag { get; set; }

        #region Constructors

        static Crawler()
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

        public Crawler()
            : this(null, null)
        {
            
        }

        public Crawler(CrawlConfiguration crawlConfiguration)
            : this(crawlConfiguration, null)
        {
            
        }

        public Crawler(ImplementationOverride implementationOverride)
            : this(null, implementationOverride)
        {

        }

        public Crawler(CrawlConfiguration crawlConfiguration, ImplementationContainer implementationContainer)
        {
            CrawlContext = new CrawlContext();
            CrawlContext.CrawlConfiguration = crawlConfiguration ?? GetCrawlConfigurationFromConfigFile();
            CrawlContext.ImplementationContainer = new ImplementationOverride(CrawlContext.CrawlConfiguration, implementationContainer);
            CrawlBag = CrawlContext.CrawlBag;

            PageRequesterEngine = CrawlContext.ImplementationContainer.PageRequesterEngine;
            PageProcessorEngine = CrawlContext.ImplementationContainer.PageProcessorEngine;
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

            CrawlContext.RootUri = uri;

            CrawlContext.CancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();

            _crawlResult = new CrawlResult();
            _crawlResult.RootUri = CrawlContext.RootUri;
            _crawlResult.CrawlContext = CrawlContext;

            _logger.InfoFormat("About to crawl site [{0}]", uri.AbsoluteUri);

            if (CrawlContext.ImplementationContainer.MemoryManager != null)
            {
                CrawlContext.MemoryUsageBeforeCrawlInMb = CrawlContext.ImplementationContainer.MemoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Starting memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, CrawlContext.MemoryUsageBeforeCrawlInMb);
            }

            PrintConfigValues(CrawlContext.CrawlConfiguration);

            CrawlContext.CrawlStartDate = DateTime.Now;
            Stopwatch timer = Stopwatch.StartNew();

            if (CrawlContext.CrawlConfiguration.CrawlTimeoutSeconds > 0)
            {
                _timeoutTimer = new System.Timers.Timer(CrawlContext.CrawlConfiguration.CrawlTimeoutSeconds * 1000);
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

            if (CrawlContext.ImplementationContainer.MemoryManager != null)
            {
                CrawlContext.MemoryUsageAfterCrawlInMb = CrawlContext.ImplementationContainer.MemoryManager.GetCurrentUsageInMb();
                _logger.InfoFormat("Ending memory usage for site [{0}] is [{1}mb]", uri.AbsoluteUri, CrawlContext.MemoryUsageAfterCrawlInMb);
            }

            _crawlResult.Elapsed = timer.Elapsed;
            _logger.InfoFormat("Crawl complete for site [{0}]: [{1}]", _crawlResult.RootUri.AbsoluteUri, _crawlResult.Elapsed);

            return _crawlResult;
        }


        protected virtual void CrawlSite(Uri uri)
        {
            CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Add(new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true });
            
            //TODO add configuration for MaxConcurrentHttpRequests
            //TODO add configuration for MaxConcurrentCrawledPageProcessors
            //TODO retire MaxConcurrentThreads

            _logger.DebugFormat("Starting PageRequesterEngine & PageProcessorEngine");
            PageRequesterEngine.Start(CrawlContext);
            PageProcessorEngine.Start(CrawlContext);

            bool crawlComplete = false;
            while (!crawlComplete)
            {
                RunPreWorkChecks();
                if (PageRequesterEngine.IsDone && PageProcessorEngine.IsDone)
                {
                    _logger.DebugFormat("Stopping PageRequesterEngine & PageProcessorEngine...");
                    PageRequesterEngine.Stop();
                    PageProcessorEngine.Stop();
                    crawlComplete = true;
                }
                else
                {
                    _logger.InfoFormat("Health check, still working...");
                    System.Threading.Thread.Sleep(2500);                
                }
            }
        }

        protected virtual void VerifyRequiredAvailableMemory()
        {
            if (CrawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb < 1)
                return;

            if (!CrawlContext.ImplementationContainer.MemoryManager.IsSpaceAvailable(CrawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb))
                throw new InsufficientMemoryException(string.Format("Process does not have the configured [{0}mb] of available memory to crawl site [{1}]. This is configurable through the minAvailableMemoryRequiredInMb in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.", CrawlContext.CrawlConfiguration.MinAvailableMemoryRequiredInMb, CrawlContext.RootUri));
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
            if (CrawlContext.ImplementationContainer.MemoryManager == null
                || CrawlContext.IsCrawlHardStopRequested
                || CrawlContext.CrawlConfiguration.MaxMemoryUsageInMb < 1)
                return;

            int currentMemoryUsage = CrawlContext.ImplementationContainer.MemoryManager.GetCurrentUsageInMb();
            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("Current memory usage for site [{0}] is [{1}mb]", CrawlContext.RootUri, currentMemoryUsage);

            if (currentMemoryUsage > CrawlContext.CrawlConfiguration.MaxMemoryUsageInMb)
            {
                CrawlContext.ImplementationContainer.MemoryManager.Dispose();
                CrawlContext.ImplementationContainer.MemoryManager = null;

                string message = string.Format("Process is using [{0}mb] of memory which is above the max configured of [{1}mb] for site [{2}]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", currentMemoryUsage, CrawlContext.CrawlConfiguration.MaxMemoryUsageInMb, CrawlContext.RootUri);
                _crawlResult.ErrorException = new InsufficientMemoryException(message);

                _logger.Fatal(_crawlResult.ErrorException);
                CrawlContext.IsCrawlHardStopRequested = true;
            }
        }

        protected virtual void CheckForCancellationRequest()
        {
            if (CrawlContext.CancellationTokenSource.IsCancellationRequested)
            {
                if (!_crawlCancellationReported)
                {
                    string message = string.Format("Crawl cancellation requested for site [{0}]!", CrawlContext.RootUri);
                    _logger.Fatal(message);
                    _crawlResult.ErrorException = new OperationCanceledException(message, CrawlContext.CancellationTokenSource.Token);
                    CrawlContext.IsCrawlHardStopRequested = true;
                    _crawlCancellationReported = true;
                }
            }
        }

        protected virtual void CheckForHardStopRequest()
        {
            //TODO Is Stop and Hard Stop needed anymore!!!!!!!!
            if (CrawlContext.IsCrawlHardStopRequested)
            {
                if (!_crawlStopReported)
                {
                    _logger.InfoFormat("Hard crawl stop requested for site [{0}]!", CrawlContext.RootUri);
                    _crawlStopReported = true;
                }

                CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Clear();
                CrawlContext.ImplementationContainer.PageRequesterEngineThreadManager.AbortAll();
                CrawlContext.ImplementationContainer.PageProcessorEngineThreadManager.AbortAll();
                CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Clear();//to be sure nothing was scheduled since first call to clear()

                PageRequesterEngine.Stop();
                PageProcessorEngine.Stop();
            }
        }

        protected virtual void CheckForStopRequest()
        {
            //TODO Is Stop and Hard Stop needed anymore!!!!!!!!
            if (CrawlContext.IsCrawlStopRequested)
            {
                if (!_crawlStopReported)
                {
                    _logger.InfoFormat("Crawl stop requested for site [{0}]!", CrawlContext.RootUri);
                    _crawlStopReported = true;
                }

                CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Clear();//to be sure nothing was scheduled since first call to clear()
                PageRequesterEngine.Stop();
                //do not stop the processor since we want to finish processing the pages that have already been crawl or started being crawled when this method was called
            }
        }

        protected virtual void HandleCrawlTimeout(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer elapsedTimer = sender as System.Timers.Timer;
            if (elapsedTimer != null)
                elapsedTimer.Stop();

            _logger.InfoFormat("Crawl timeout of [{0}] seconds has been reached for [{1}]", CrawlContext.CrawlConfiguration.CrawlTimeoutSeconds, CrawlContext.RootUri);
            CrawlContext.IsCrawlHardStopRequested = true;
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

        protected virtual CrawlConfiguration GetCrawlConfigurationFromConfigFile()
        {
            AbotConfigurationSectionHandler configFromFile = AbotConfigurationSectionHandler.LoadFromXml();

            if (configFromFile == null)
                throw new InvalidOperationException("abot config section was NOT found");

            _logger.DebugFormat("abot config section was found");
            return configFromFile.Convert();
        }
    }
}
