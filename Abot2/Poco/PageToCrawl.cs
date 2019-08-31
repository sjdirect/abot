using System;
using System.Dynamic;

namespace Abot2.Poco
{
    public class PageToCrawl
    {
        //Needed for serialization
        public PageToCrawl()
        {
        }
         
        public PageToCrawl(Uri uri)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            PageBag = new ExpandoObject();
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
        /// The time in seconds that the server sent to wait before retrying.
        /// </summary>
        public double? RetryAfter { get; set; }

        /// <summary>
        /// The number of times the http request was be retried.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// The datetime that the last http request was made. Will be null unless retries are enabled.
        /// </summary>
        public DateTime? LastRequest { get; set; }

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

        /// <summary>
        /// Can store values of any type. Useful for adding custom values to the CrawledPage dynamically from event subscriber code
        /// </summary>
        public dynamic PageBag { get; set; }

        /// <summary>
        /// The uri that this page was redirected from. If null then it was not part of the redirect chain
        /// </summary>
        public CrawledPage RedirectedFrom { get; set; }

        /// <summary>
        /// The position in the redirect chain. The first redirect is position 1, the next one is 2 and so on.
        /// </summary>
        public int RedirectPosition { get; set; }

        public override string ToString()
        {
            return Uri.AbsoluteUri;
        }
    }
}
