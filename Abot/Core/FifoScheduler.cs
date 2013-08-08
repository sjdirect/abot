using Abot.Poco;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Abot.Core
{
    /// <summary>
    /// Handles managing the priority of what pages need to be crawled
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Count of remaining items that are currently scheduled
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Schedules the param to be crawled
        /// </summary>
        void Add(PageToCrawl page);

        /// <summary>
        /// Schedules the param to be crawled
        /// </summary>
        void Add(IEnumerable<PageToCrawl> pages);

        /// <summary>
        /// Gets the next page to crawl
        /// </summary>
        PageToCrawl GetNext();

        /// <summary>
        /// Clear all currently scheduled pages
        /// </summary>
        void Clear();
    }

    public class FifoScheduler : IScheduler
    {
        static ILog _logger = LogManager.GetLogger(typeof(FifoScheduler).FullName);
        ConcurrentQueue<PageToCrawl> _pagesToCrawl = new ConcurrentQueue<PageToCrawl>();
        ConcurrentDictionary<string, object> _scheduledOrCrawled = new ConcurrentDictionary<string, object>();
         
        bool _allowUriRecrawling = false;

        public FifoScheduler()
        {
        }

        public FifoScheduler(bool allowUriRecrawling)
        {
            _allowUriRecrawling = allowUriRecrawling;
        }

        /// <summary>
        /// Count of remaining items that are currently scheduled
        /// </summary>
        public int Count
        {
            get
            {
                return _pagesToCrawl.Count;
            }
        }

        /// <summary>
        /// Schedules the param to be crawled in a FIFO fashion
        /// </summary>
        public void Add(PageToCrawl page)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            if (_allowUriRecrawling)
            {
                //_logger.DebugFormat("Scheduling for crawl [{0}]", page.Uri.AbsoluteUri);
                _pagesToCrawl.Enqueue(page);
            }
            else
            {
                if (_scheduledOrCrawled.TryAdd(page.Uri.AbsoluteUri, null))
                {
                    //_logger.DebugFormat("Scheduling for crawl [{0}]", page.Uri.AbsoluteUri);
                    _pagesToCrawl.Enqueue(page);
                }
            }
        }

        public void Add(IEnumerable<PageToCrawl> pages)
        {
            if(pages == null)
                throw new ArgumentNullException("pages");

            foreach (PageToCrawl page in pages)
                Add(page);
        }

        /// <summary>
        /// Gets the next page to crawl
        /// </summary>
        public PageToCrawl GetNext()
        {
            PageToCrawl nextItem = null;

            if(_pagesToCrawl.Count > 0)//issue 14: have to check this again since it may have changed since calling this method
                _pagesToCrawl.TryDequeue(out nextItem);

            return nextItem;
        }

        /// <summary>
        /// Clear all currently scheduled pages
        /// </summary>
        public void Clear()
        {
            _pagesToCrawl = new ConcurrentQueue<PageToCrawl>();
        }
    }
}
