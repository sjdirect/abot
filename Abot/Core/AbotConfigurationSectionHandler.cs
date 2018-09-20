using Abot.Poco;
using System;
using System.Configuration;
using System.Runtime.Remoting.Channels;

namespace Abot.Core
{
    [Serializable]
    public class AbotConfigurationSectionHandler : ConfigurationSection
    {
        public AbotConfigurationSectionHandler()
        {
            
        }

        [ConfigurationProperty("crawlBehavior")]
        public CrawlBehaviorElement CrawlBehavior
        {
            get { return (CrawlBehaviorElement)this["crawlBehavior"]; }
        }

        [ConfigurationProperty("politeness")]
        public PolitenessElement Politeness
        {
            get { return (PolitenessElement)this["politeness"]; }
        }

        [ConfigurationProperty("authorization")]
        public AuthorizationElement Authorization
        {
            get { return (AuthorizationElement)this["authorization"]; }
        }

        [ConfigurationProperty("extensionValues")]
        [ConfigurationCollection(typeof(ExtensionValueCollection), AddItemName = "add")]
        public ExtensionValueCollection ExtensionValues
        {
            get { return (ExtensionValueCollection)this["extensionValues"]; }
        }

        public CrawlConfiguration Convert()
        {
            CrawlConfiguration config = new CrawlConfiguration();
            Map(CrawlBehavior, config);
            Map(Politeness, config);
            Map(Authorization, config);

            foreach (ExtensionValueElement element in ExtensionValues)
                config.ConfigurationExtensions.Add(element.Key, element.Value);

            return config;
        }

        private void Map(CrawlBehaviorElement src, CrawlConfiguration dest)
        {
            dest.MaxConcurrentThreads = src.MaxConcurrentThreads;
            dest.MaxPagesToCrawl = src.MaxPagesToCrawl;
            dest.MaxPagesToCrawlPerDomain = src.MaxPagesToCrawlPerDomain;
            dest.MaxPageSizeInBytes = src.MaxPageSizeInBytes;
            dest.UserAgentString = src.UserAgentString;
            dest.HttpProtocolVersion = GetHttpProtocolVersion(src);
            dest.CrawlTimeoutSeconds = src.CrawlTimeoutSeconds;
            dest.IsUriRecrawlingEnabled = src.IsUriRecrawlingEnabled;
            dest.IsExternalPageCrawlingEnabled = src.IsExternalPageCrawlingEnabled;
            dest.IsExternalPageLinksCrawlingEnabled = src.IsExternalPageLinksCrawlingEnabled;
            dest.IsRespectUrlNamedAnchorOrHashbangEnabled = src.IsRespectUrlNamedAnchorOrHashbangEnabled;
            dest.DownloadableContentTypes = src.DownloadableContentTypes;
            dest.HttpServicePointConnectionLimit = src.HttpServicePointConnectionLimit;
            dest.HttpRequestTimeoutInSeconds = src.HttpRequestTimeoutInSeconds;
            dest.HttpRequestMaxAutoRedirects = src.HttpRequestMaxAutoRedirects;
            dest.IsHttpRequestAutoRedirectsEnabled = src.IsHttpRequestAutoRedirectsEnabled;
            dest.IsHttpRequestAutomaticDecompressionEnabled = src.IsHttpRequestAutomaticDecompressionEnabled;
            dest.IsSendingCookiesEnabled = src.IsSendingCookiesEnabled;
            dest.IsSslCertificateValidationEnabled = src.IsSslCertificateValidationEnabled;
            dest.MinAvailableMemoryRequiredInMb = src.MinAvailableMemoryRequiredInMb;
            dest.MaxMemoryUsageInMb = src.MaxMemoryUsageInMb;
            dest.MaxMemoryUsageCacheTimeInSeconds = src.MaxMemoryUsageCacheTimeInSeconds;
            dest.MaxCrawlDepth = src.MaxCrawlDepth;
            dest.MaxLinksPerPage = src.MaxLinksPerPage;
            dest.IsForcedLinkParsingEnabled = src.IsForcedLinkParsingEnabled;
            dest.MaxRetryCount = src.MaxRetryCount;
            dest.MinRetryDelayInMilliseconds = src.MinRetryDelayInMilliseconds;
        }

        private void Map(PolitenessElement src, CrawlConfiguration dest)
        {
            dest.IsRespectRobotsDotTextEnabled = src.IsRespectRobotsDotTextEnabled;
            dest.IsRespectMetaRobotsNoFollowEnabled = src.IsRespectMetaRobotsNoFollowEnabled;
            dest.IsRespectHttpXRobotsTagHeaderNoFollowEnabled = src.IsRespectHttpXRobotsTagHeaderNoFollowEnabled;
            dest.IsRespectAnchorRelNoFollowEnabled = src.IsRespectAnchorRelNoFollowEnabled;
            dest.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = src.IsIgnoreRobotsDotTextIfRootDisallowedEnabled;
            dest.RobotsDotTextUserAgentString = src.RobotsDotTextUserAgentString;
            dest.MinCrawlDelayPerDomainMilliSeconds = src.MinCrawlDelayPerDomainMilliSeconds;
            dest.MaxRobotsDotTextCrawlDelayInSeconds = src.MaxRobotsDotTextCrawlDelayInSeconds;
        }

        private void Map(AuthorizationElement src, CrawlConfiguration dest)
        {
            dest.IsAlwaysLogin = src.IsAlwaysLogin;
            dest.LoginUser = src.LoginUser;
            dest.LoginPassword = src.LoginPassword;
            dest.UseDefaultCredentials = src.UseDefaultCredentials;
        }

        private HttpProtocolVersion GetHttpProtocolVersion(CrawlBehaviorElement src)
        {

            switch (src.HttpProtocolVersion)
            {
                case "1.0":
                    return HttpProtocolVersion.Version10;
                case "1.1":
                    return HttpProtocolVersion.Version11;
                default:
                    return HttpProtocolVersion.NotSpecified;
            }
        }

        public static AbotConfigurationSectionHandler LoadFromXml()
        {
            return ((AbotConfigurationSectionHandler)System.Configuration.ConfigurationManager.GetSection("abot"));
        }
    }


    [Serializable]
    public class AuthorizationElement : ConfigurationElement
    {
        /// <summary>
        /// Defines whatewer each request shold be autorized via login 
        /// </summary>
        [ConfigurationProperty("isAlwaysLogin", IsRequired = false)]
        public bool IsAlwaysLogin
        {
            get { return (bool)this["isAlwaysLogin"]; }
        }

        /// <summary>
        /// The user name to be used for autorization 
        /// </summary>
        [ConfigurationProperty("loginUser", IsRequired = false)]
        public string LoginUser
        {
            get { return (string)this["loginUser"]; }
        }
        /// <summary>
        /// The password to be used for autorization 
        /// </summary>
        [ConfigurationProperty("loginPassword", IsRequired = false)]
        public string LoginPassword
        {
            get { return (string)this["loginPassword"]; }
        }

        /// <summary>
        /// Specifies whether to use default credentials. 
        /// </summary>
        [ConfigurationProperty("useDefaultCredentials", IsRequired = false)]
        public bool UseDefaultCredentials
        {
            get { return (bool)this["useDefaultCredentials"]; }
        }
    }
    [Serializable]
    public class PolitenessElement : ConfigurationElement
    {
        [ConfigurationProperty("isRespectRobotsDotTextEnabled", IsRequired = false)]
        public bool IsRespectRobotsDotTextEnabled
        {
            get { return (bool)this["isRespectRobotsDotTextEnabled"]; }
        }

        [ConfigurationProperty("isRespectMetaRobotsNoFollowEnabled", IsRequired = false)]
        public bool IsRespectMetaRobotsNoFollowEnabled
        {
            get { return (bool)this["isRespectMetaRobotsNoFollowEnabled"]; }
        }

        [ConfigurationProperty("isRespectHttpXRobotsTagHeaderNoFollowEnabled", IsRequired = false)]
        public bool IsRespectHttpXRobotsTagHeaderNoFollowEnabled
        {
            get { return (bool)this["isRespectHttpXRobotsTagHeaderNoFollowEnabled"]; }
        }

        [ConfigurationProperty("isRespectAnchorRelNoFollowEnabled", IsRequired = false)]
        public bool IsRespectAnchorRelNoFollowEnabled
        {
            get { return (bool)this["isRespectAnchorRelNoFollowEnabled"]; }
        }

        [ConfigurationProperty("isIgnoreRobotsDotTextIfRootDisallowedEnabled", IsRequired = false)]
        public bool IsIgnoreRobotsDotTextIfRootDisallowedEnabled
        {
            get { return (bool)this["isIgnoreRobotsDotTextIfRootDisallowedEnabled"]; }
        }

        [ConfigurationProperty("robotsDotTextUserAgentString", IsRequired = false, DefaultValue = "abot")]
        public string RobotsDotTextUserAgentString
        {
            get { return (string)this["robotsDotTextUserAgentString"]; }
        }

        [ConfigurationProperty("maxRobotsDotTextCrawlDelayInSeconds", IsRequired = false, DefaultValue = 5)]
        public int MaxRobotsDotTextCrawlDelayInSeconds
        {
            get { return (int)this["maxRobotsDotTextCrawlDelayInSeconds"]; }
        }

        [ConfigurationProperty("minCrawlDelayPerDomainMilliSeconds", IsRequired = false)]
        public int MinCrawlDelayPerDomainMilliSeconds
        {
            get { return (int)this["minCrawlDelayPerDomainMilliSeconds"]; }
        }
    }

    [Serializable]
    public class CrawlBehaviorElement : ConfigurationElement
    {
        [ConfigurationProperty("maxConcurrentThreads", IsRequired = false, DefaultValue = 10)]
        public int MaxConcurrentThreads
        {
            get { return (int)this["maxConcurrentThreads"]; }
        }

        [ConfigurationProperty("maxPagesToCrawl", IsRequired = false, DefaultValue = 1000)]
        public int MaxPagesToCrawl
        {
            get { return (int)this["maxPagesToCrawl"]; }
        }

        [ConfigurationProperty("maxPagesToCrawlPerDomain", IsRequired = false)]
        public int MaxPagesToCrawlPerDomain
        {
            get { return (int)this["maxPagesToCrawlPerDomain"]; }
        }

        [ConfigurationProperty("maxPageSizeInBytes", IsRequired = false)]
        public int MaxPageSizeInBytes
        {
            get { return (int)this["maxPageSizeInBytes"]; }
        }

        [ConfigurationProperty("userAgentString", IsRequired = false, DefaultValue = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko")]
        public string UserAgentString
        {
            get { return (string)this["userAgentString"]; }
        }

        [ConfigurationProperty("httpProtocolVersion", IsRequired = false)]
        public string HttpProtocolVersion
        {
            get{ return (string)this["httpProtocolVersion"]; }
        }

        [ConfigurationProperty("crawlTimeoutSeconds", IsRequired = false)]
        public int CrawlTimeoutSeconds
        {
            get { return (int)this["crawlTimeoutSeconds"]; }
        }

        [ConfigurationProperty("downloadableContentTypes", IsRequired = false, DefaultValue = "text/html")]
        public string DownloadableContentTypes
        {
            get { return (string)this["downloadableContentTypes"]; }
        }

        [ConfigurationProperty("isUriRecrawlingEnabled", IsRequired = false)]
        public bool IsUriRecrawlingEnabled
        {
            get { return (bool)this["isUriRecrawlingEnabled"]; }
        }

        [ConfigurationProperty("isExternalPageCrawlingEnabled", IsRequired = false)]
        public bool IsExternalPageCrawlingEnabled
        {
            get { return (bool)this["isExternalPageCrawlingEnabled"]; }
        }

        [ConfigurationProperty("isExternalPageLinksCrawlingEnabled", IsRequired = false)]
        public bool IsExternalPageLinksCrawlingEnabled
        {
            get { return (bool)this["isExternalPageLinksCrawlingEnabled"]; }
        }

        [ConfigurationProperty("isSslCertificateValidationEnabled", IsRequired = false, DefaultValue = true)]
        public bool IsSslCertificateValidationEnabled
        {
            get { return (bool)this["isSslCertificateValidationEnabled"]; }
        }

        [ConfigurationProperty("httpServicePointConnectionLimit", IsRequired = false, DefaultValue = 200)]
        public int HttpServicePointConnectionLimit
        {
            get { return (int)this["httpServicePointConnectionLimit"]; }
        }

        [ConfigurationProperty("httpRequestTimeoutInSeconds", IsRequired = false, DefaultValue = 15)]
        public int HttpRequestTimeoutInSeconds
        {
            get { return (int)this["httpRequestTimeoutInSeconds"]; }
        }

        [ConfigurationProperty("httpRequestMaxAutoRedirects", IsRequired = false, DefaultValue = 7)]
        public int HttpRequestMaxAutoRedirects
        {
            get { return (int)this["httpRequestMaxAutoRedirects"]; }
        }

        [ConfigurationProperty("isHttpRequestAutoRedirectsEnabled", IsRequired = false, DefaultValue = true)]
        public bool IsHttpRequestAutoRedirectsEnabled
        {
            get { return (bool)this["isHttpRequestAutoRedirectsEnabled"]; }
        }

        [ConfigurationProperty("isHttpRequestAutomaticDecompressionEnabled", IsRequired = false)]
        public bool IsHttpRequestAutomaticDecompressionEnabled
        {
            get { return (bool)this["isHttpRequestAutomaticDecompressionEnabled"]; }
        }

        [ConfigurationProperty("isSendingCookiesEnabled", IsRequired = false)]
        public bool IsSendingCookiesEnabled
        {
            get { return (bool)this["isSendingCookiesEnabled"]; }
        }

        [ConfigurationProperty("isRespectUrlNamedAnchorOrHashbangEnabled", IsRequired = false)]
        public bool IsRespectUrlNamedAnchorOrHashbangEnabled
        {
            get { return (bool)this["isRespectUrlNamedAnchorOrHashbangEnabled"]; }
        }

        [ConfigurationProperty("minAvailableMemoryRequiredInMb", IsRequired = false)]
        public int MinAvailableMemoryRequiredInMb
        {
            get { return (int)this["minAvailableMemoryRequiredInMb"]; }
        }

        [ConfigurationProperty("maxMemoryUsageInMb", IsRequired = false)]
        public int MaxMemoryUsageInMb
        {
            get { return (int)this["maxMemoryUsageInMb"]; }
        }

        [ConfigurationProperty("maxMemoryUsageCacheTimeInSeconds", IsRequired = false)]
        public int MaxMemoryUsageCacheTimeInSeconds
        {
            get { return (int)this["maxMemoryUsageCacheTimeInSeconds"]; }
        }

        [ConfigurationProperty("maxCrawlDepth", IsRequired = false, DefaultValue = 100)]
        public int MaxCrawlDepth
        {
            get { return (int)this["maxCrawlDepth"]; }
        }

        [ConfigurationProperty("maxLinksPerPage", IsRequired = false, DefaultValue = 0)]
        public int MaxLinksPerPage
        {
            get { return (int)this["maxLinksPerPage"]; }
        }

        [ConfigurationProperty("isForcedLinkParsingEnabled", IsRequired = false)]
        public bool IsForcedLinkParsingEnabled
        {
            get { return (bool)this["isForcedLinkParsingEnabled"]; }
        }

        [ConfigurationProperty("maxRetryCount", IsRequired = false)]
        public int MaxRetryCount
        {
            get { return (int)this["maxRetryCount"]; }
        }

        [ConfigurationProperty("minRetryDelayInMilliseconds", IsRequired = false)]
        public int MinRetryDelayInMilliseconds
        {
            get { return (int)this["minRetryDelayInMilliseconds"]; }
        }
    }

    [Serializable]
    public class ExtensionValueElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = false, IsKey = true)]
        public string Key
        {
            get { return (string)this["key"]; }
        }

        [ConfigurationProperty("value", IsRequired = false, IsKey = false)]
        public string Value
        {
            get { return (string)this["value"]; }
        }

    }

    [Serializable]
    public class ExtensionValueCollection : ConfigurationElementCollection
    {
        public ExtensionValueElement this[int index]
        {
            get { return (ExtensionValueElement)BaseGet(index); }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ExtensionValueElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExtensionValueElement)element).Key;
        }
    }
}
