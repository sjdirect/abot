﻿using System;
using System.Dynamic;

namespace Abot.Poco
{
    [Serializable]
    public class PageToCrawl
    {
        //Needed for serialization
        public PageToCrawl()
        {
        }

        public PageToCrawl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            Uri = uri;
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

        public override string ToString()
        {
            return Uri.AbsoluteUri;
        }
    }
}
