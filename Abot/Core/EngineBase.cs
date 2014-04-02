using Abot.Core;
using Abot.Poco;
using log4net;
using System;
using System.Threading;

namespace Abot.Core
{
    public abstract class EngineBase
    {
        static ILog _logger = LogManager.GetLogger(typeof(EngineBase).FullName);
        
        protected CrawlConfiguration CrawlConfiguration { get; set; }
        
        protected ImplementationContainer ImplementationContainer { get; set; }
        
        protected CrawlContext CrawlContext { get; set; }      

        protected CancellationTokenSource CancellationTokenSource { get; set; }

        public EngineBase(CrawlConfiguration crawlConfiguration, ImplementationContainer implementationContainer)
        {
            if(crawlConfiguration == null)
                throw new ArgumentNullException("crawlCofiguration");

            if (implementationContainer == null)
                throw new ArgumentNullException("implementationContainer");

            CrawlConfiguration = crawlConfiguration;
            ImplementationContainer = implementationContainer;
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the Engine
        /// </summary>
        public virtual void Start(CrawlContext crawlContext)
        {
            if (crawlContext == null)
                throw new ArgumentNullException("crawlContext");

            CrawlContext = crawlContext;
        }

        protected virtual void FirePageActionStartingEvent(CrawlContext crawlContext, EventHandler<PageActionStartingArgs> eventHander, PageToCrawl pageToCrawl, string eventDisplayName)
        {
            try
            {
                EventHandler<PageActionStartingArgs> threadSafeEvent = eventHander;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageActionStartingArgs(crawlContext, pageToCrawl));
            }
            catch (Exception e)
            {
                LogException(e, pageToCrawl, eventDisplayName);
            }
        }

        protected virtual void FirePageActionStartingEventAsync(CrawlContext crawlContext, EventHandler<PageActionStartingArgs> eventHandler, PageToCrawl pageToCrawl, string eventDisplayName)
        {
            EventHandler<PageActionStartingArgs> threadSafeEvent = eventHandler;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageActionStartingArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageActionStartingArgs(crawlContext, pageToCrawl), null, null);
                }
            }
        }

        protected virtual void FirePageActionCompletedEvent(CrawlContext crawlContext, EventHandler<PageActionCompletedArgs> eventHandler, CrawledPage crawledPage, string eventDisplayName)
        {
            try
            {
                EventHandler<PageActionCompletedArgs> threadSafeEvent = eventHandler;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageActionCompletedArgs(crawlContext, crawledPage));
            }
            catch (Exception e)
            {
                LogException(e, crawledPage, eventDisplayName);
            }
        }

        protected virtual void FirePageActionCompletedEventAsync(CrawlContext crawlContext, EventHandler<PageActionCompletedArgs> eventHandler, CrawledPage crawledPage, string eventDisplayName)
        {
            EventHandler<PageActionCompletedArgs> threadSafeEvent = eventHandler;

            if (threadSafeEvent == null)
                return;

            if (crawlContext.ImplementationContainer.PagesToCrawlScheduler.Count == 0)
            {
                //Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
                try
                {
                    threadSafeEvent(this, new PageActionCompletedArgs(crawlContext, crawledPage));
                }
                catch (Exception e)
                {
                    LogException(e, crawledPage, eventDisplayName);
                }
            }
            else
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageActionCompletedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageActionCompletedArgs(crawlContext, crawledPage), null, null);
                }
            }
        }

        protected virtual void FirePageActionDisallowedEvent(CrawlContext crawlContext, EventHandler<PageActionDisallowedArgs> eventHandler, PageToCrawl pageToCrawl, string eventDisplayName, string reason)
        {
            try
            {
                EventHandler<PageActionDisallowedArgs> threadSafeEvent = eventHandler;
                if (threadSafeEvent != null)
                    threadSafeEvent(this, new PageActionDisallowedArgs(crawlContext, pageToCrawl, reason));
            }
            catch (Exception e)
            {
                LogException(e, pageToCrawl, eventDisplayName);
            }
        }

        protected virtual void FirePageActionDisallowedEventAsync(CrawlContext crawlContext, EventHandler<PageActionDisallowedArgs> eventHandler, PageToCrawl pageToCrawl, string eventDisplayName, string reason)
        {
            EventHandler<PageActionDisallowedArgs> threadSafeEvent = eventHandler;

            if (threadSafeEvent == null)
                return;

            if (crawlContext.ImplementationContainer.PagesToCrawlScheduler.Count == 0)
            {
                //Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
                try
                {
                    threadSafeEvent(this, new PageActionDisallowedArgs(crawlContext, pageToCrawl, reason));
                }
                catch (Exception e)
                {
                    LogException(e, pageToCrawl, eventDisplayName);
                }
            }
            else
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageActionDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageActionDisallowedArgs(crawlContext, pageToCrawl, reason), null, null);
                }
            }
        }

        private void LogException(Exception e, PageToCrawl pageToCrawl, string eventDisplayName)
        {
            _logger.ErrorFormat("An unhandled exception was thrown by a subscriber of the event [{0}] for url [{1}]", eventDisplayName, pageToCrawl.Uri.AbsoluteUri);
            _logger.Error(e);
        }
    }
}
