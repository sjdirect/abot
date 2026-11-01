using Abot2.Poco;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Abot2.Core
{
    public interface IPagesToCrawlRepository : IDisposable
    {
        void Add(PageToCrawl page);
        PageToCrawl GetNext();
        void Clear();
        int Count();

    }

    public class FifoPagesToCrawlRepository : IPagesToCrawlRepository
    {
        private ConcurrentQueue<PageToCrawl> _urlQueue = new ConcurrentQueue<PageToCrawl>();

        public void Add(PageToCrawl page)
        {
            GetQueue().Enqueue(page);
        }

        public PageToCrawl GetNext()
        {
            PageToCrawl pageToCrawl;
            GetQueue().TryDequeue(out pageToCrawl);

            return pageToCrawl;
        }

        public void Clear()
        {
            var queue = GetQueue();

            //Compare-and-swap so a concurrent Dispose() cannot be undone by putting a new queue back
            if (Interlocked.CompareExchange(ref _urlQueue, new ConcurrentQueue<PageToCrawl>(), queue) != queue)
                GetQueue();//throws if disposed meanwhile, otherwise another Clear() already replaced the queue
        }

        public int Count()
        {
            return GetQueue().Count;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _urlQueue = null;
        }

        private ConcurrentQueue<PageToCrawl> GetQueue()
        {
            return _urlQueue ?? throw new ObjectDisposedException(GetType().FullName);
        }
    }

}
