using Abot2.Poco;
using System;

namespace Abot2.Crawler
{
    public class PageLinksCrawlDisallowedArgs : PageCrawlCompletedArgs
    {
        public string DisallowedReason { get; }

        public PageLinksCrawlDisallowedArgs(CrawlContext crawlContext, CrawledPage crawledPage, string disallowedReason)
            : base(crawlContext, crawledPage)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException(nameof(disallowedReason));

            DisallowedReason = disallowedReason;
        }
    }
}
