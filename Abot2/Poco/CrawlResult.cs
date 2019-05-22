using System;

namespace Abot2.Poco
{
    public class CrawlResult
    {
        public CrawlResult()
        {
        }

        /// <summary>
        /// The root of the crawl
        /// </summary>
        public Uri RootUri { get; set; }

        /// <summary>
        /// The amount of time that elapsed before the crawl completed
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// Whether or not an error occurred during the crawl that caused it to end prematurely
        /// </summary>
        public bool ErrorOccurred 
        {
            get
            {
                return (ErrorException != null);
            }
        }

        /// <summary>
        /// The exception that caused the crawl to end prematurely
        /// </summary>
        public Exception ErrorException { get; set; }

        /// <summary>
        /// The context of the crawl
        /// </summary>
        public CrawlContext CrawlContext { get; set; }
    }
}
