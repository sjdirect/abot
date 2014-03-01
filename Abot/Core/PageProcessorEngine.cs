
namespace Abot.Core
{
    using Abot.Poco;
    using System;
    using System.Threading;
    
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

        void Start(CrawlContext crawlContext, CancellationTokenSource cancellationTokenSource);

        bool IsDone { get; }
    }

    public class PageProcessorEngine : IPageProcessorEngine
    {
        public PageProcessorEngine()
        {
            //IsInternalUriShortcut = (uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;
        }

        //this also needs an instance of CrawlDecision to determine if the page links will be added

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool IsDone
        {
            get { throw new NotImplementedException(); }
        }

        public void Start(CrawlContext crawlContext, CancellationTokenSource cancellationTokenSource)
        {
            throw new NotImplementedException();
        }
    }
}
