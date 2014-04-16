using Abot.Poco;
using log4net;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace Abot.Core
{
    public interface IEngine
    {
        /// <summary>
        /// Whether the engine has completed all actions.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Starts taking actions on pages and firing events
        /// </summary>
        /// <param name="crawlContext">The context of the crawl</param>
        void Start(CrawlContext crawlContext);

        /// <summary>
        /// Stops making http requests and firing events
        /// </summary>
        void Stop();
    }

    public interface IPageRequesterEngine : IEngine, IDisposable
    {
        /// <summary>
        /// Synchronous event that is fired before an http request is sent for a page.
        /// </summary>
        event EventHandler<PageActionStartingArgs> PageRequestStarting;

        /// <summary>
        /// Synchronous event that is fired after an http request is complete for a page.
        /// </summary>
        event EventHandler<PageActionCompletedArgs> PageRequestCompleted;

        /// <summary>
        /// Asynchronous event that is fired before an http request is sent for a page.
        /// </summary>
        event EventHandler<PageActionStartingArgs> PageRequestStartingAsync;

        /// <summary>
        /// Asynchronous event that is fired  afteran http request is complete for a page.
        /// </summary>
        event EventHandler<PageActionCompletedArgs> PageRequestCompletedAsync;
    }

    /// <summary>
    /// Makes http requests for all items in the CrawlContext.PagesToCrawl collection and fires events.
    /// </summary>
    public class PageRequesterEngine : EngineBase, IPageRequesterEngine
    {
        static ILog _logger = LogManager.GetLogger(typeof(PageRequesterEngine).FullName);
        
        /// <summary>
        /// IPageRequester implementation that is used to make raw http requests
        /// </summary>
        public bool IsDone
        {
            get
            {
                _logger.DebugFormat("IsCancelled: {0}, ThreadsRunning: {1}, PagesToCrawl: {2}", CancellationTokenSource.Token.IsCancellationRequested, ImplementationContainer.PageRequesterEngineThreadManager.HasRunningThreads(), CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Count);
                return (CancellationTokenSource.Token.IsCancellationRequested ||
                    (!ImplementationContainer.PageRequesterEngineThreadManager.HasRunningThreads() && 
                    CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Count == 0));
            }
        }

        /// <summary>
        /// hronous event that is fired before a page is crawled.
        /// </summary>
        public event EventHandler<PageActionStartingArgs> PageRequestStarting;

        /// <summary>
        /// hronous event that is fired when an individual page has been crawled.
        /// </summary>
        public event EventHandler<PageActionCompletedArgs> PageRequestCompleted;

        /// <summary>
        /// Asynchronous event that is fired before a page is crawled.
        /// </summary>
        public event EventHandler<PageActionStartingArgs> PageRequestStartingAsync;

        /// <summary>
        /// Asynchronous event that is fired when an individual page has been crawled.
        /// </summary>
        public event EventHandler<PageActionCompletedArgs> PageRequestCompletedAsync;


        public PageRequesterEngine(CrawlConfiguration crawlConfiguration, ImplementationContainer implementationContainer)
            : base(crawlConfiguration, implementationContainer)
        {
        }


        /// <summary>
        /// Starts the PageRequesterEngine
        /// </summary>
        public virtual void Start(CrawlContext crawlContext)
        {
            if(crawlContext == null)
                throw new ArgumentNullException("crawlContext");

            CrawlContext = crawlContext;

            _logger.InfoFormat("PageRequesterEngine starting, [{0}] pages left to request", CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Count);

            //TODO should this task be "LongRunning"
            Task.Factory.StartNew(() =>
            {
                MakeRequests();
            });
        }

        /// <summary>
        /// Stops the PageRequesterEngine
        /// </summary>
        public virtual void Stop()
        {
            _logger.InfoFormat("PageRequesterEngine stopping, [{0}] pages left to request", CrawlContext.ImplementationContainer.PagesToCrawlScheduler.Count);
            CancellationTokenSource.Cancel();
            Dispose();
        }

        public virtual void Dispose()
        {
            ImplementationContainer.PageRequesterEngineThreadManager.AbortAll();

            //Set all events to null so no more events are fired
            PageRequestStarting = null;
            PageRequestCompleted = null;
            PageRequestStartingAsync = null;
            PageRequestCompletedAsync = null;
        }

        protected virtual void MakeRequests()
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (ImplementationContainer.PagesToCrawlScheduler.Count > 0)
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();
                    ImplementationContainer.PageRequesterEngineThreadManager.DoWork(() => MakeRequest(ImplementationContainer.PagesToCrawlScheduler.GetNext()));
                }
                else
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    _logger.DebugFormat("Waiting for pages to crawl...");
                    System.Threading.Thread.Sleep(500);
                }
            }

            _logger.DebugFormat("Done making http requests");
        }

        protected internal virtual void MakeRequest(PageToCrawl pageToCrawl)
        {
            try
            {
                if (pageToCrawl == null)
                    return;

                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                _logger.DebugFormat("About to request [{0}], [{1}] pages left to request", pageToCrawl.Uri, ImplementationContainer.PagesToCrawlScheduler.Count);

                base.FirePageActionStartingEventAsync(CrawlContext, PageRequestStartingAsync, pageToCrawl, "PageRequestStartingAsync");
                base.FirePageActionStartingEvent(CrawlContext, PageRequestStarting, pageToCrawl, "PageRequestStarting");

                CrawledPage crawledPage = CrawlThePage(pageToCrawl);

                if (PageSizeIsAboveMax(crawledPage))
                    return;

                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                ImplementationContainer.PagesToProcessScheduler.Add(crawledPage);

                base.FirePageActionCompletedEventAsync(CrawlContext, PageRequestCompletedAsync, crawledPage, "PageRequestCompletedAsync");
                base.FirePageActionCompletedEvent(CrawlContext, PageRequestCompleted, crawledPage, "PageRequestCompleted");
            }
            catch (OperationCanceledException oce)
            {
                _logger.DebugFormat("Thread cancelled while requesting page [{0}]", pageToCrawl.Uri);
                throw;
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Error occured making http request to page [{0}]", pageToCrawl.Uri);
                _logger.Error(e);
            }
        }

        protected virtual bool PageSizeIsAboveMax(CrawledPage crawledPage)
        {
            bool isAboveMax = false;
            if (CrawlContext.CrawlConfiguration.MaxPageSizeInBytes > 0 &&
                crawledPage.Content.Bytes != null &&
                crawledPage.Content.Bytes.Length > CrawlContext.CrawlConfiguration.MaxPageSizeInBytes)
            {
                isAboveMax = true;
                _logger.DebugFormat("Page [{0}] has a page size of [{1}] bytes which is above the [{2}] byte max", crawledPage.Uri, crawledPage.Content.Bytes.Length, CrawlContext.CrawlConfiguration.MaxPageSizeInBytes);
            }
            return isAboveMax;
        }

        protected virtual CrawledPage CrawlThePage(PageToCrawl pageToCrawl)
        {
            _logger.DebugFormat("Requesting page [{0}]", pageToCrawl.Uri.AbsoluteUri);

            CrawledPage crawledPage = ImplementationContainer.PageRequester.MakeRequest(pageToCrawl.Uri, (x) => ImplementationContainer.CrawlDecisionMaker.ShouldDownloadPageContent(x, CrawlContext));
            dynamic combinedPageBag = CombinePageBags(pageToCrawl.PageBag, crawledPage.PageBag);
            AutoMapper.Mapper.CreateMap<PageToCrawl, CrawledPage>();
            AutoMapper.Mapper.Map(pageToCrawl, crawledPage);
            crawledPage.PageBag = combinedPageBag;

            if (crawledPage.HttpWebResponse == null)
                _logger.InfoFormat("Page request complete, Status:[NA] Url:[{0}] Parent:[{1}]", crawledPage.Uri.AbsoluteUri, crawledPage.ParentUri);
            else
                _logger.InfoFormat("Page request complete, Status:[{0}] Url:[{1}] Parent:[{2}]", Convert.ToInt32(crawledPage.HttpWebResponse.StatusCode), crawledPage.Uri.AbsoluteUri, crawledPage.ParentUri);

            return crawledPage;
        }

        protected virtual dynamic CombinePageBags(dynamic pageToCrawlBag, dynamic crawledPageBag)
        {
            IDictionary<string, object> combinedBag = new ExpandoObject();
            var pageToCrawlBagDict = pageToCrawlBag as IDictionary<string, object>;
            var crawledPageBagDict = crawledPageBag as IDictionary<string, object>;

            foreach (KeyValuePair<string, object> entry in pageToCrawlBagDict) combinedBag[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, object> entry in crawledPageBagDict) combinedBag[entry.Key] = entry.Value;

            return combinedBag;
        }
    }
}
