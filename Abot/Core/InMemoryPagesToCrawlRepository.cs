using Abot.Poco;
using System;

namespace Abot.Core
{
    public interface IPagesToCrawlRepository
    {
        void Add(PageToCrawl page);
        PageToCrawl GetNext();
        void Clear();
        int Count();

    }

    public class InMemoryPagesToCrawlRepository : IPagesToCrawlRepository
    {
        public void Add(PageToCrawl page)
        {
            throw new NotImplementedException();
        }

        public PageToCrawl GetNext()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public int Count()
        {
            throw new NotImplementedException();
        }
    }

}
