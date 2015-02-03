using Abot.Poco;
using System;
using System.Configuration;

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

        [ConfigurationProperty("extensionValues")]
        [ConfigurationCollection(typeof(ExtensionValueCollection), AddItemName = "add")]
        public ExtensionValueCollection ExtensionValues
        {
            get { return (ExtensionValueCollection)this["extensionValues"]; }
        }

        public CrawlConfiguration Convert()
        {
            AutoMapper.Mapper.CreateMap<CrawlBehaviorElement, CrawlConfiguration>();
            AutoMapper.Mapper.CreateMap<PolitenessElement, CrawlConfiguration>();


            CrawlConfiguration config = new CrawlConfiguration();
            AutoMapper.Mapper.Map<CrawlBehaviorElement, CrawlConfiguration>(CrawlBehavior, config);
            AutoMapper.Mapper.Map<PolitenessElement, CrawlConfiguration>(Politeness, config);

            foreach (ExtensionValueElement element in ExtensionValues)
                config.ConfigurationExtensions.Add(element.Key, element.Value);

            return config;
        }

        public static AbotConfigurationSectionHandler LoadFromXml()
        {
            return ((AbotConfigurationSectionHandler)System.Configuration.ConfigurationManager.GetSection("abot"));
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

        [ConfigurationProperty("isRespectAnchorRelNoFollowEnabled", IsRequired = false)]
        public bool IsRespectAnchorRelNoFollowEnabled
        {
            get { return (bool)this["isRespectAnchorRelNoFollowEnabled"]; }
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
