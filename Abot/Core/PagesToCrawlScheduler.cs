using Abot.Poco;
using System;
using System.Collections.Generic;

namespace Abot.Core
{
    /// <summary>
    /// Handles managing the priority of items to work on
    /// </summary>
    public interface IScheduler<T>
    {
        /// <summary>
        /// Count of remaining items that are currently scheduled
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Schedules the item
        /// </summary>
        void Add(T item);

        /// <summary>
        /// Schedules the items
        /// </summary>
        void Add(IEnumerable<T> items);

        /// <summary>
        /// Gets the next item
        /// </summary>
        T GetNext();

        /// <summary>
        /// Clear all currently scheduled items
        /// </summary>
        void Clear();
    }

    //TODO Need a more generic scheduler like ItemScheduler<T>
    public class PagesToCrawlScheduler : IScheduler<PageToCrawl>
    {
        ICrawledUrlRepository _crawledUrlRepo;
        IPagesToCrawlRepository _pagesToCrawlRepo;
        bool _allowUriRecrawling;

        public PagesToCrawlScheduler()
            :this(false, null, null)
        {
        }

        public PagesToCrawlScheduler(bool allowUriRecrawling, ICrawledUrlRepository crawledUrlRepo, IPagesToCrawlRepository pagesToCrawlRepo)
        {
            _allowUriRecrawling = allowUriRecrawling;
            _crawledUrlRepo = crawledUrlRepo ?? new MemoryBloomUrlRepository();
            _pagesToCrawlRepo = pagesToCrawlRepo ?? new FifoPagesToCrawlRepository();
        }

        public int Count
        {
            get { return _pagesToCrawlRepo.Count(); }
        }

        public void Add(PageToCrawl page)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            if (_allowUriRecrawling || page.IsRetry)
            {
                _pagesToCrawlRepo.Add(page);
            }
            else
            {
                if (_crawledUrlRepo.AddIfNew(page.Uri))
                    _pagesToCrawlRepo.Add(page);
            }
        }

        public void Add(IEnumerable<PageToCrawl> pages)
        {
            if (pages == null)
                throw new ArgumentNullException("pages");

            foreach (PageToCrawl page in pages)
                Add(page);
        }

        public PageToCrawl GetNext()
        {
            return _pagesToCrawlRepo.GetNext();
        }

        public void Clear()
        {
            _pagesToCrawlRepo.Clear();
        }
    }
}
