﻿using Abot.Poco;
using System;
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

    [Serializable]
    public class Scheduler : IScheduler
    {
        ICrawledUrlRepository _crawledUrlRepo;
        IPagesToCrawlRepository _pagesToCrawlRepo;
        bool _allowUriRecrawling;

        public Scheduler()
            :this(false, null, null)
        {
        }

        public Scheduler(bool allowUriRecrawling, ICrawledUrlRepository crawledUrlRepo, IPagesToCrawlRepository pagesToCrawlRepo)
        {
            _allowUriRecrawling = allowUriRecrawling;
            _crawledUrlRepo = crawledUrlRepo ?? new InMemoryCrawledUrlRepository();
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
