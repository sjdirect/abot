using Abot.Crawler;
using Abot.Poco;
using log4net;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace Abot.Core
{
    public interface IPageRequesterEngine: IDisposable
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

        /// <summary>
        /// Whether the engine has completed making all http requests for all the PageToCrawl objects.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// Starts making http requests and firing events
        /// </summary>
        /// <param name="crawlContext">The context of the crawl</param>
        /// <param name="shouldRetrieveResponseBody">Delegate to call to determine if the response body should be retrieved</param>
        void Start(CrawlContext crawlContext, Func<CrawledPage, CrawlContext, CrawlDecision> shouldRetrieveResponseBody);

        /// <summary>
        /// Stops making http requests and firing events
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Makes http requests for all items in the CrawlContext.PagesToCrawl collection and fires events.
    /// </summary>
    public class PageRequesterEngine : EngineBase, IPageRequesterEngine
    {
        static ILog _logger = LogManager.GetLogger(typeof(PageRequesterEngine).FullName);
        
        /// <summary>
        /// IThreadManager implementation that is used to manage multithreading
        /// </summary>
        public IThreadManager ThreadManager { get; set; }

        /// <summary>
        /// IPageRequester implementation that is used to make raw http requests
        /// </summary>
        public IPageRequester PageRequester { get; set; }

        /// <summary>
        /// IPageRequester implementation that is used to make raw http requests
        /// </summary>
        public bool IsDone
        {
            get
            {
                return (CancellationTokenSource.Token.IsCancellationRequested || 
                    (!ThreadManager.HasRunningThreads() && CrawlContext.PagesToCrawl.Count == 0));
            }
        }

        /// <summary>
        /// Registers a delegate to be called to determine if the response body should be retrieved the page
        /// </summary>
        public Func<CrawledPage, CrawlDecision> ShouldRetrieveResponseBody { get; set; }

        /// <summary>
        /// CrawlContext that is used to decide what to crawl and how
        /// </summary>
        public CrawlContext CrawlContext { get; set; }

        /// <summary>
        /// Cancellation token used to shut down the engine
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Creates instance of PageRequesterEngine using default implemention of dependencies.
        /// </summary>
        public PageRequesterEngine()
            : this(null, null, null)
        {
            
        }

        /// <summary>
        /// Creates instance of HttpRequestEngine. Passing null for any value will use the default implementation.
        /// </summary>
        public PageRequesterEngine(
            CrawlConfiguration crawlConfiguration,
            IThreadManager threadManager,
            IPageRequester httpRequester)
        {
            CrawlConfiguration config = crawlConfiguration ?? new CrawlConfiguration();

            ThreadManager = threadManager ?? new TaskThreadManager(config.MaxConcurrentThreads);
            PageRequester = httpRequester ?? new PageRequester(config);
            CancellationTokenSource = new CancellationTokenSource();
        }
        //TODO This should not get a delegate, it should just take ICrawlDecisionMaker in the constructor
        /// <summary>
        /// Starts the HttpRequestEngine
        /// </summary>
        public virtual void Start(CrawlContext crawlContext, Func<CrawledPage, CrawlContext, CrawlDecision> shouldRetrieveReponseBody)
        {
            if(crawlContext == null)
                throw new ArgumentNullException("crawlContext");

            CrawlContext = crawlContext;

            _logger.InfoFormat("HttpRequestEngine starting, [{0}] pages left to request", CrawlContext.PagesToCrawl.Count);

            //TODO should this task be "LongRunning"
            Task.Factory.StartNew(() =>
            {
                foreach (PageToCrawl pageToCrawl in CrawlContext.PagesToCrawl.GetConsumingEnumerable())
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();
                    _logger.DebugFormat("About to request [{0}], [{1}] pages left to request", pageToCrawl.Uri, CrawlContext.PagesToCrawl.Count);
                    ThreadManager.DoWork(() => MakeRequest(pageToCrawl));
                }
                _logger.DebugFormat("Complete requesting pages");
            });
        }

        /// <summary>
        /// Stops the HttpRequestEngine
        /// </summary>
        public virtual void Stop()
        {
            _logger.InfoFormat("HttpRequestEngine stopping, [{0}] pages left to request", CrawlContext.PagesToCrawl.Count);
            CancellationTokenSource.Cancel();
            Dispose();
        }

        public virtual void Dispose()
        {
            ThreadManager.AbortAll();

            //Set all events to null so no more events are fired
            PageRequestStarting = null;
            PageRequestCompleted = null;
            PageRequestStartingAsync = null;
            PageRequestCompletedAsync = null;
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

        protected internal virtual void MakeRequest(PageToCrawl pageToCrawl)
        {
            try
            {
                if (pageToCrawl == null)
                    return;

                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                base.FirePageActionStartingEventAsync(CrawlContext, PageRequestStartingAsync, pageToCrawl, "PageRequestStartingAsync");
                base.FirePageActionStartingEvent(CrawlContext, PageRequestStarting, pageToCrawl, "PageRequestStarting");

                CrawledPage crawledPage = CrawlThePage(pageToCrawl);

                if (PageSizeIsAboveMax(crawledPage))
                    return;

                CancellationTokenSource.Token.ThrowIfCancellationRequested();

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
            _logger.DebugFormat("About to crawl page [{0}]", pageToCrawl.Uri.AbsoluteUri);

            //CrawledPage crawledPage = PageRequester.MakeRequest(pageToCrawl.Uri, (x) => ShouldRetrieveResponseBody);//TODO need to implement this!!!
            CrawledPage crawledPage = PageRequester.MakeRequest(pageToCrawl.Uri);
            dynamic combinedPageBag = CombinePageBags(pageToCrawl.PageBag, crawledPage.PageBag);
            AutoMapper.Mapper.CreateMap<PageToCrawl, CrawledPage>();
            AutoMapper.Mapper.Map(pageToCrawl, crawledPage);
            crawledPage.PageBag = combinedPageBag;

            if (crawledPage.HttpWebResponse == null)
                _logger.InfoFormat("Page crawl complete, Status:[NA] Url:[{0}] Parent:[{1}]", crawledPage.Uri.AbsoluteUri, crawledPage.ParentUri);
            else
                _logger.InfoFormat("Page crawl complete, Status:[{0}] Url:[{1}] Parent:[{2}]", Convert.ToInt32(crawledPage.HttpWebResponse.StatusCode), crawledPage.Uri.AbsoluteUri, crawledPage.ParentUri);

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
