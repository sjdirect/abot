using Abot.Poco;
using System;

namespace Abot.Core
{
    public class PageActionStartingArgs : CrawlArgs
    {
        public PageToCrawl PageToCrawl { get; private set; }

        public PageActionStartingArgs(CrawlContext crawlContext, PageToCrawl pageToCrawl)
            : base(crawlContext)
        {
            if (pageToCrawl == null)
                throw new ArgumentNullException("pageToCrawl");

            PageToCrawl = pageToCrawl;
        }
    }
}
