using Abot.Poco;
using System;

namespace Abot.Crawler
{
    [Serializable]
    public class PageCrawlDisallowedArgs: PageCrawlStartingArgs
    {
        public string DisallowedReason { get; private set; }

        public PageCrawlDisallowedArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl, string disallowedReason)
            : base(crawlContext, pageToCrawl)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException("disallowedReason");

            DisallowedReason = disallowedReason;
        }
    }
}
