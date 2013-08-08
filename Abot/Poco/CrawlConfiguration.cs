using System.Collections.Generic;

namespace Abot.Poco
{
    public class CrawlConfiguration
    {
        public CrawlConfiguration()
        {
            MaxConcurrentThreads = 10;
            UserAgentString = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; abot v@ABOTASSEMBLYVERSION@ http://code.google.com/p/abot)";
            RobotsDotTextUserAgentString = "abot";
            MaxPagesToCrawl = 1000;
            DownloadableContentTypes = "text/html";
            ConfigurationExtensions = new Dictionary<string, string>();
            MaxRobotsDotTextCrawlDelayInSeconds = 5;
            HttpRequestMaxAutoRedirects = 7;
            IsHttpRequestAutoRedirectsEnabled = true;
            MaxCrawlDepth = 100;
        }

        #region crawlBehavior

        /// <summary>
        /// Max concurrent threads to use for http requests
        /// </summary>
        public int MaxConcurrentThreads { get; set; }

        /// <summary>
        /// Maximum number of pages to crawl.
        /// This value is required.
        /// </summary>
        public long MaxPagesToCrawl { get; set; }

        /// <summary>
        /// Maximum number of pages to crawl per domain
        /// If zero, this setting has no effect.
        /// </summary>
        public long MaxPagesToCrawlPerDomain { get; set; }

        /// <summary>
        /// Maximum size of page. If the page size is above this value, it will not be downloaded or processed
        /// If zero, this setting has no effect.
        /// </summary>
        public long MaxPageSizeInBytes { get; set; }

        /// <summary>
        /// The maximum numer of seconds to respect in the robots.txt "Crawl-delay: X" directive. 
        /// IsRespectRobotsDotTextEnabled must be true for this value to be used.
        /// If zero, will use whatever the robots.txt crawl delay requests no matter how high the value is.
        /// </summary>
        public int MaxRobotsDotTextCrawlDelayInSeconds { get; set; }

        /// <summary>
        /// The user agent string to use for http requests
        /// </summary>
        public string UserAgentString { get; set; }

        /// <summary>
        /// Maximum seconds before the crawl times out and stops. 
        /// If zero, this setting has no effect.
        /// </summary>
        public long CrawlTimeoutSeconds { get; set; }

        /// <summary>
        /// Dictionary that stores additional keyvalue pairs that can be accessed throught the crawl pipeline
        /// </summary>
        public Dictionary<string, string> ConfigurationExtensions { get; set; }

        /// <summary>
        /// Whether Uris should be crawled more than once. This is not common and should be false for most scenarios
        /// </summary>
        public bool IsUriRecrawlingEnabled { get; set; }

        /// <summary>
        /// Whether pages external to the root uri should be crawled
        /// </summary>
        public bool IsExternalPageCrawlingEnabled { get; set; }

        /// <summary>
        /// Whether pages external to the root uri should have their links crawled. NOTE: IsExternalPageCrawlEnabled must be true for this setting to have any effect
        /// </summary>
        public bool IsExternalPageLinksCrawlingEnabled { get; set; }

        /// <summary>
        /// A comma seperated string that has content types that should have their page content downloaded. For each page, the content type is checked to see if it contains any of the values defined here.
        /// </summary>
        public string DownloadableContentTypes { get; set; } 

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections allowed by a System.Net.ServicePoint. The system default is 2. This means that only 2 concurrent http connections can be open to the same host.
        /// If zero, this setting has no effect.
        /// </summary>
        public int HttpServicePointConnectionLimit { get; set; }

        /// <summary>
        /// Gets or sets the time-out value in milliseconds for the System.Net.HttpWebRequest.GetResponse() and System.Net.HttpWebRequest.GetRequestStream() methods.
        /// If zero, this setting has no effect.
        /// </summary>
        public int HttpRequestTimeoutInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of redirects that the request follows.
        /// If zero, this setting has no effect.
        /// </summary>
        public int HttpRequestMaxAutoRedirects { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the request should follow redirection
        /// </summary>
        public bool IsHttpRequestAutoRedirectsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates gzip and deflate will be automatically accepted and decompressed
        /// </summary>
        public bool IsHttpRequestAutomaticDecompressionEnabled { get; set; }

        /// <summary>
        /// Uses closest mulitple of 16 to the value set. If there is not at least this much memory available before starting a crawl, throws InsufficientMemoryException.
        /// If zero, this setting has no effect.
        /// </summary>
        /// <exception cref="http://msdn.microsoft.com/en-us/library/system.insufficientmemoryexception.aspx">InsufficientMemoryException</exception>
        public int MinAvailableMemoryRequiredInMb { get; set; }

        /// <summary>
        /// The max amout of memory to allow the process to use. If this limit is exceeded the crawler will stop prematurely.
        /// If zero, this setting has no effect.
        /// </summary>
        public int MaxMemoryUsageInMb { get; set; }

        /// <summary>
        /// The max amount of time before refreshing the value used to determine the amount of memory being used by the process that hosts the crawler instance.
        /// This value has no effect if MaxMemoryUsageInMb is zero.
        /// </summary>
        public int MaxMemoryUsageCacheTimeInSeconds { get; set; }

        /// <summary>
        /// Maximum levels below root page to crawl. If value is 0, the homepage will be crawled but none of its links will be crawled. If the level is 1, the homepage and its links will be crawled but none of the links links will be crawled.
        /// </summary>
        public int MaxCrawlDepth { get; set; }

        #endregion

        #region politeness

        /// <summary>
        /// Whether the crawler should retrieve and respect the robotsdottext file.
        /// </summary>
        public bool IsRespectRobotsDotTextEnabled { get; set; }

        /// <summary>
        /// The user agent string to use when checking robots.txt file for specific directives.  Some examples of other crawler's user agent values are "googlebot", "slurp" etc...
        /// </summary>
        public string RobotsDotTextUserAgentString { get; set; }

        /// <summary>
        /// The number of milliseconds to wait in between http requests to the same domain. Note: This will set the crawl to a single thread no matter what the MaxConcurrentThreads value is.
        /// </summary>
        public long MinCrawlDelayPerDomainMilliSeconds { get; set; }
        
        #endregion
    }
}
