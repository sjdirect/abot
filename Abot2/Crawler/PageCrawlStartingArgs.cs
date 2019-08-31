using Abot2.Poco;
using System;

namespace Abot2.Crawler
{
    public class PageCrawlStartingArgs : CrawlArgs
    {
        public PageToCrawl PageToCrawl { get; private set; }

        public PageCrawlStartingArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl)
            : base(crawlContext)
        {
            PageToCrawl = pageToCrawl ?? throw new ArgumentNullException(nameof(pageToCrawl));
        }
    }
}
