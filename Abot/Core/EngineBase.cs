using Abot.Crawler;
using Abot.Poco;
using System;

namespace Abot.Core
{
    public interface IEngine
    {
        ///// <summary>
        ///// The description of the action that is taken before/after each page ie.. 'http request' 
        ///// </summary>
        //public string ActionShortDescription { get; set; }

        /// <summary>
        /// Synchronous event that is fired before starting to take an action on a page.
        /// </summary>
        event EventHandler<PageActionStartingArgs> PageActionStarting;

        /// <summary>
        /// Synchronous event that is fired after completing an action on a page.
        /// </summary>
        event EventHandler<PageActionCompletedArgs> PageActionCompleted;

        /// <summary>
        /// Synchronous event that is fired after determining that an action should not be taken on a page.
        /// </summary>
        event EventHandler<PageActionDisallowedArgs> PageActionDisallowed;

        /// <summary>
        /// Asynchronous event that is fired before starting to take an action on a page.
        /// </summary>        
        event EventHandler<PageActionStartingArgs> PageActionStartingAsync;

        /// <summary>
        /// Asynchronous event that is fired after completing an action on a page.
        /// </summary>
        event EventHandler<PageActionCompletedArgs> PageActionCompletedAsync;

        /// <summary>
        /// Asynchronous event that is fired after determining that an action should not be taken on a page.
        /// </summary>
        event EventHandler<PageActionDisallowedArgs> PageActionDisallowedAsync;
    }

    public abstract class EngineBase : IEngine
    {
        private string _exceptionMessageFormat = "An unhandled exception was thrown by a subscriber of the event [{0}] for url [{1}]";

        /// <summary>
        /// Synchronous event that is fired before an action is performed on a page.
        /// </summary>
        public event EventHandler<PageActionStartingArgs> PageActionStarting;

        /// <summary>
        /// Asynchronous event that is fired before an action is performed on a page.
        /// </summary>
        public event EventHandler<PageActionStartingArgs> PageActionStartingAsync;


        /// <summary>
        /// Asynchronous event that is fired after an action has been performed on a page.
        /// </summary>
        public event EventHandler<PageActionCompletedArgs> PageActionCompleted;

        /// <summary>
        /// Asynchronous event that is fired after an action has been performed on a page.
        /// </summary>
        public event EventHandler<PageActionCompletedArgs> PageActionCompletedAsync;

        protected virtual void FirePageActionStartingEvent(PageToCrawl pageToCrawl, string eventDisplayName)
        {
            try
            {
                EventHandler<PageActionStartingArgs> threadSafeEvent = PageActionStarting;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageActionStartingArgs(CrawlContext, pageToCrawl));
            }
            catch (Exception e)
            {
                _logger.ErrorFormat(_exceptionMessageFormat, eventDisplayName, pageToCrawl.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

        protected virtual void FirePageActionStartingEventAsync(PageToCrawl pageToCrawl)
        {
            EventHandler<PageActionStartingArgs> threadSafeEvent = PageActionStartingAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageActionStartingArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageActionStartingArgs(CrawlContext, pageToCrawl), null, null);
                }
            }
        }


        protected virtual void FirePageActionCompletedEvent(CrawledPage crawledPage, string eventDisplayName)
        {
            try
            {
                EventHandler<PageActionCompletedArgs> threadSafeEvent = PageActionCompleted;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageActionCompletedArgs(CrawlContext, crawledPage));
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("An unhandled exception was thrown by a subscriber of the PageActionCompleted event [{0}] for url [{1}]", actionName, pageToCrawl.Uri.AbsoluteUri);
                _logger.Error(e);
            }
        }

        protected virtual void FirePageActionCompletedEventAsync(CrawledPage crawledPage)
        {
            EventHandler<PageActionCompletedArgs> threadSafeEvent = PageActionCompletedAsync;

            if (threadSafeEvent == null)
                return;

            if (CrawlContext.PagesToCrawl.Count == 0)
            {
                //Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
                try
                {
                    threadSafeEvent(this, new PageActionCompletedArgs(CrawlContext, crawledPage));
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
                foreach (EventHandler<PageActionCompletedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageActionCompletedArgs(CrawlContext, crawledPage), null, null);
                }
            }
        }
    }
}
