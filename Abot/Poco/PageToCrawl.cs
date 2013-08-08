using System;

namespace Abot.Poco
{
    public class PageToCrawl
    {
        public PageToCrawl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            Uri = uri;
        }

        /// <summary>
        /// The uri of the page
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// The parent uri of the page
        /// </summary>
        public Uri ParentUri { get; set; }

        /// <summary>
        /// Whether http requests had to be retried more than once. This could be due to throttling or politeness.
        /// </summary>
        public bool IsRetry { get; set; }

        /// <summary>
        /// Whether the page is the root uri of the crawl
        /// </summary>
        public bool IsRoot { get; set; }

        /// <summary>
        /// Whether the page is internal to the root uri of the crawl
        /// </summary>
        public bool IsInternal { get; set; }


        /// <summary>
        /// The depth from the root of the crawl. If this page is the homepage this value will be zero, if this page was found on the homepage this value will be 1 and so on.
        /// </summary>
        public int CrawlDepth { get; set; }

        public override string ToString()
        {
            return Uri.AbsoluteUri;
        }
    }
}
