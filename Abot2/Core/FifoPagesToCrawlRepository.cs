using Abot2.Poco;
using System;
using System.Collections.Concurrent;

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
        internal ConcurrentQueue<PageToCrawl> UrlQueue = new ConcurrentQueue<PageToCrawl>();

        public void Add(PageToCrawl page)
        {
            UrlQueue.Enqueue(page);
        }

        public PageToCrawl GetNext()
        {
            PageToCrawl pageToCrawl;
            UrlQueue.TryDequeue(out pageToCrawl);

            return pageToCrawl;
        }

        public void Clear()
        {
            UrlQueue = new ConcurrentQueue<PageToCrawl>();
        }

        public int Count()
        {
            return UrlQueue.Count;
        }

        public virtual void Dispose()
        {
            UrlQueue = null;
        }
    }

}
