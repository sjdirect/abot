using Abot2.Poco;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Abot2.Core
{
    /// <summary>
    /// Handles managing the priority of what pages need to be crawled
    /// </summary>
    public interface IScheduler : IDisposable
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

        /// <summary>
        /// Add the Url to the list of crawled Url without scheduling it to be crawled.
        /// Returns true if the Url was not known before. This is atomic, so of several callers racing for the same Url exactly one gets true,
        /// which lets that caller alone evaluate the Url.
        /// </summary>
        /// <param name="uri"></param>
        bool AddKnownUri(Uri uri);

        /// <summary>
        /// Schedules a page whose Url the caller has already marked as known with <see cref="AddKnownUri"/>, without checking for a duplicate again
        /// </summary>
        void AddKnownPage(PageToCrawl page);

        /// <summary>
        /// Returns whether or not the specified Uri was already scheduled to be crawled or simply added to the
        /// list of known Uris.
        /// </summary>
        bool IsUriKnown(Uri uri);
    }

    public class Scheduler : IScheduler
    {
        ICrawledUrlRepository _crawledUrlRepo;
        IPagesToCrawlRepository _pagesToCrawlRepo;
        bool _allowUriRecrawling;
        readonly bool _ownsCrawledUrlRepo;
        readonly bool _ownsPagesToCrawlRepo;
        int _isDisposed;

        public Scheduler()
            :this(false, null, null)
        {
        }

        /// <param name="allowUriRecrawling">Whether a uri that was already scheduled may be scheduled again</param>
        /// <param name="crawledUrlRepo">Owned by the caller, who must dispose it; if null, an internal one is created and disposed with this instance.</param>
        /// <param name="pagesToCrawlRepo">Owned by the caller, who must dispose it; if null, an internal one is created and disposed with this instance.</param>
        public Scheduler(bool allowUriRecrawling, ICrawledUrlRepository crawledUrlRepo, IPagesToCrawlRepository pagesToCrawlRepo)
        {
            _allowUriRecrawling = allowUriRecrawling;
            _ownsCrawledUrlRepo = crawledUrlRepo == null;
            _ownsPagesToCrawlRepo = pagesToCrawlRepo == null;
            _crawledUrlRepo = crawledUrlRepo ?? new CompactCrawledUrlRepository();
            _pagesToCrawlRepo = pagesToCrawlRepo ?? new FifoPagesToCrawlRepository();
        }

        public int Count
        {
            get
            {
                ThrowIfDisposed();
                return _pagesToCrawlRepo.Count();
            }
        }

        public void Add(PageToCrawl page)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            ThrowIfDisposed();

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

            foreach (var page in pages)
                Add(page);
        }

        public PageToCrawl GetNext()
        {
            ThrowIfDisposed();
            return _pagesToCrawlRepo.GetNext();
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _pagesToCrawlRepo.Clear();
        }

        public bool AddKnownUri(Uri uri)
        {
            ThrowIfDisposed();
            return _crawledUrlRepo.AddIfNew(uri);
        }

        public void AddKnownPage(PageToCrawl page)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            ThrowIfDisposed();
            _pagesToCrawlRepo.Add(page);
        }

        public bool IsUriKnown(Uri uri)
        {
            ThrowIfDisposed();
            return _crawledUrlRepo.Contains(uri);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (disposing)
            {
                if (_ownsCrawledUrlRepo)
                    _crawledUrlRepo.Dispose();
                if (_ownsPagesToCrawlRepo)
                    _pagesToCrawlRepo.Dispose();
            }
        }

        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
