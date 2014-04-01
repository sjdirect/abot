using Abot.Poco;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Abot.Core
{
    //TODO Add unit test for this and modify tests for other scheduler!!!
    public class PagesToProcessScheduler : IScheduler<CrawledPage>
    {
        ConcurrentQueue<CrawledPage> _pagesToProcess = new ConcurrentQueue<CrawledPage>();

        public int Count
        {
            get { return _pagesToProcess.Count; }
        }

        public void Add(CrawledPage page)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            _pagesToProcess.Add(page);
        }

        public void Add(IEnumerable<CrawledPage> pages)
        {
            if (pages == null)
                throw new ArgumentNullException("pages");

            foreach (CrawledPage page in pages)
                Add(page);
        }

        public PageToCrawl GetNext()
        {
            return _pagesToProcess.GetNext();
        }

        public void Clear()
        {
            _pagesToProcess = new ConcurrentQueue<CrawledPage>();
        }
    }
}
