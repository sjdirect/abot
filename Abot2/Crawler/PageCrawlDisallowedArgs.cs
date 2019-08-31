using Abot2.Poco;
using System;

namespace Abot2.Crawler
{
    public class PageCrawlDisallowedArgs: PageCrawlStartingArgs
    {
        public string DisallowedReason { get; private set; }

        public PageCrawlDisallowedArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl, string disallowedReason)
            : base(crawlContext, pageToCrawl)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException(nameof(disallowedReason));

            DisallowedReason = disallowedReason;
        }
    }
}
