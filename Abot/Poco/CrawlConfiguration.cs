﻿using System;
using System.Collections.Generic;

namespace Abot.Poco
{
    [Serializable]
    public class CrawlConfiguration
    {
        public CrawlConfiguration()
        {
            MaxConcurrentThreads = 10;
            UserAgentString = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
            RobotsDotTextUserAgentString = "abot";
            MaxPagesToCrawl = 1000;
            DownloadableContentTypes = "text/html";
            ConfigurationExtensions = new Dictionary<string, string>();
            MaxRobotsDotTextCrawlDelayInSeconds = 5;
            HttpRequestMaxAutoRedirects = 7;
            IsHttpRequestAutoRedirectsEnabled = true;
            MaxCrawlDepth = 100;
            HttpServicePointConnectionLimit = 200;
            HttpRequestTimeoutInSeconds = 15;
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
        public int MaxPagesToCrawl { get; set; }

        /// <summary>
        /// Maximum number of pages to crawl per domain
        /// If zero, this setting has no effect.
        /// </summary>
        public int MaxPagesToCrawlPerDomain { get; set; }

        /// <summary>
        /// Maximum size of page. If the page size is above this value, it will not be downloaded or processed
        /// If zero, this setting has no effect.
        /// </summary>
        public int MaxPageSizeInBytes { get; set; }

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
        public int CrawlTimeoutSeconds { get; set; }

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
        /// Whether the cookies should be set and resent with every request
        /// </summary>
        public bool IsSendingCookiesEnabled { get; set; }

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

        /// <summary>
        /// Gets or sets a value that indicates whether the crawler should parse the page's links even if a CrawlDecision (like CrawlDecisionMaker.ShouldCrawlPageLinks()) determines that those links will not be crawled.
        /// </summary>
        public bool IsForcedLinkParsingEnabled { get; set; }

        /// <summary>
        /// The max number of retries for a url if a web exception is encountered. If the value is 0, no retries will be made
        /// </summary>
        public int MaxRetryCount { get; set; }

        /// <summary>
        /// The minimum delay between a failed http request and the next retry
        /// </summary>
        public int MinRetryDelayInMilliseconds { get; set; }

        #endregion

        #region politeness

        /// <summary>
        /// Whether the crawler should retrieve and respect the robots.txt file.
        /// </summary>
        public bool IsRespectRobotsDotTextEnabled { get; set; }

        /// <summary>
        /// Whether the crawler should ignore links on pages that have a <meta name="robots" content="nofollow" /> tag
        /// </summary>
        public bool IsRespectMetaRobotsNoFollowEnabled { get; set; }

        /// <summary>
        /// Whether the crawler should ignore links that have a <a href="whatever" rel="nofollow">...
        /// </summary>
        public bool IsRespectAnchorRelNoFollowEnabled { get; set; }

        /// <summary>
        /// The user agent string to use when checking robots.txt file for specific directives.  Some examples of other crawler's user agent values are "googlebot", "slurp" etc...
        /// </summary>
        public string RobotsDotTextUserAgentString { get; set; }

        /// <summary>
        /// The number of milliseconds to wait in between http requests to the same domain.
        /// </summary>
        public int MinCrawlDelayPerDomainMilliSeconds { get; set; }
        
        #endregion
    }
}
