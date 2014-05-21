using Abot.Poco;
using System;

namespace Abot.Core
{
    public class PageActionCompletedArgs : CrawlArgs
    {
        public CrawledPage CrawledPage { get; private set; }

        public PageActionCompletedArgs(CrawlContext crawlContext, CrawledPage crawledPage)
            : base(crawlContext)
        {
            if (crawledPage == null)
                throw new ArgumentNullException("crawledPage");

            CrawledPage = crawledPage;
        }
    }
}
