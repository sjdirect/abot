using Abot2.Poco;
using System;

namespace Abot2.Crawler
{
    public class CrawlArgs : EventArgs
    {
        public CrawlContext CrawlContext { get; set; }

        public CrawlArgs(CrawlContext crawlContext)
        {
            CrawlContext = crawlContext ?? throw new ArgumentNullException(nameof(crawlContext));
        }
    }
}
