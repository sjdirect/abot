using Abot.Poco;
using System;

namespace Abot.Core
{
    public class PageActionDisallowedArgs: PageActionStartingArgs
    {
        public string DisallowedReason { get; private set; }

        public PageActionDisallowedArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl, string disallowedReason)
            : base(crawlContext, pageToCrawl)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException("disallowedReason");

            DisallowedReason = disallowedReason;
        }
    }
}
