
using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class AbotConfigurationSectionHandlerTest
    {
        AbotConfigurationSectionHandler _uut = AbotConfigurationSectionHandler.LoadFromXml();

        [Test]
        public void GetSetion_FillsConfigValuesFromAppConfigFile()
        {
            Assert.IsNotNull(_uut.CrawlBehavior);
            Assert.AreEqual(44, _uut.CrawlBehavior.CrawlTimeoutSeconds);
            Assert.AreEqual("bbbb", _uut.CrawlBehavior.DownloadableContentTypes);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsUriRecrawlingEnabled);
            Assert.AreEqual(11, _uut.CrawlBehavior.MaxConcurrentThreads);
            Assert.AreEqual(33, _uut.CrawlBehavior.MaxPagesToCrawl);
            Assert.AreEqual(333, _uut.CrawlBehavior.MaxPagesToCrawlPerDomain);
            Assert.AreEqual(4444, _uut.CrawlBehavior.MaxPageSizeInBytes);
            Assert.AreEqual("aaaa", _uut.CrawlBehavior.UserAgentString);
            Assert.AreEqual("1.0", _uut.CrawlBehavior.HttpProtocolVersion);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsExternalPageCrawlingEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsExternalPageLinksCrawlingEnabled);
            Assert.AreEqual(21, _uut.CrawlBehavior.HttpServicePointConnectionLimit);
            Assert.AreEqual(22, _uut.CrawlBehavior.HttpRequestTimeoutInSeconds);
            Assert.AreEqual(23, _uut.CrawlBehavior.HttpRequestMaxAutoRedirects);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsHttpRequestAutoRedirectsEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsHttpRequestAutomaticDecompressionEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsSendingCookiesEnabled);
            Assert.AreEqual(false, _uut.CrawlBehavior.IsSslCertificateValidationEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsRespectUrlNamedAnchorOrHashbangEnabled);
            Assert.AreEqual(25, _uut.CrawlBehavior.MinAvailableMemoryRequiredInMb);
            Assert.AreEqual(26, _uut.CrawlBehavior.MaxMemoryUsageInMb);
            Assert.AreEqual(27, _uut.CrawlBehavior.MaxMemoryUsageCacheTimeInSeconds);
            Assert.AreEqual(28, _uut.CrawlBehavior.MaxCrawlDepth);
            Assert.AreEqual(29, _uut.CrawlBehavior.MaxLinksPerPage);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsForcedLinkParsingEnabled);
            Assert.AreEqual(4, _uut.CrawlBehavior.MaxRetryCount);
            Assert.AreEqual(4444, _uut.CrawlBehavior.MinRetryDelayInMilliseconds);

            Assert.IsNotNull(_uut.Politeness);
            Assert.AreEqual(true, _uut.Politeness.IsRespectRobotsDotTextEnabled);
            Assert.AreEqual(true, _uut.Politeness.IsRespectMetaRobotsNoFollowEnabled);
            Assert.AreEqual(true, _uut.Politeness.IsRespectAnchorRelNoFollowEnabled);
            Assert.AreEqual(true, _uut.Politeness.IsRespectHttpXRobotsTagHeaderNoFollowEnabled);
            Assert.AreEqual(true, _uut.Politeness.IsIgnoreRobotsDotTextIfRootDisallowedEnabled);
            Assert.AreEqual("zzzz", _uut.Politeness.RobotsDotTextUserAgentString);
            Assert.AreEqual(55, _uut.Politeness.MinCrawlDelayPerDomainMilliSeconds);
            Assert.AreEqual(5, _uut.Politeness.MaxRobotsDotTextCrawlDelayInSeconds);

            Assert.IsNotNull(_uut.ExtensionValues);
            Assert.AreEqual("key1", _uut.ExtensionValues[0].Key);
            Assert.AreEqual("key2", _uut.ExtensionValues[1].Key);
            Assert.AreEqual("value1", _uut.ExtensionValues[0].Value);
            Assert.AreEqual("value2", _uut.ExtensionValues[1].Value);
        }

        [Test]
        public void Convert_CovertsFromSectionObjectToDtoObject()
        {
            CrawlConfiguration result = _uut.Convert();

            Assert.IsNotNull(result);
            Assert.AreEqual(result.CrawlTimeoutSeconds, _uut.CrawlBehavior.CrawlTimeoutSeconds);
            Assert.AreEqual(result.DownloadableContentTypes, _uut.CrawlBehavior.DownloadableContentTypes);
            Assert.AreEqual(result.IsUriRecrawlingEnabled, _uut.CrawlBehavior.IsUriRecrawlingEnabled);
            Assert.AreEqual(result.MaxConcurrentThreads, _uut.CrawlBehavior.MaxConcurrentThreads);
            Assert.AreEqual(result.MaxPagesToCrawl, _uut.CrawlBehavior.MaxPagesToCrawl);
            Assert.AreEqual(result.MaxPagesToCrawlPerDomain, _uut.CrawlBehavior.MaxPagesToCrawlPerDomain);
            Assert.AreEqual(result.MaxPageSizeInBytes, _uut.CrawlBehavior.MaxPageSizeInBytes);
            Assert.AreEqual(result.UserAgentString, _uut.CrawlBehavior.UserAgentString);
            Assert.AreEqual(result.HttpProtocolVersion, HttpProtocolVersion.Version10);
            Assert.AreEqual(result.IsExternalPageCrawlingEnabled, _uut.CrawlBehavior.IsExternalPageCrawlingEnabled);
            Assert.AreEqual(result.IsExternalPageLinksCrawlingEnabled, _uut.CrawlBehavior.IsExternalPageLinksCrawlingEnabled);
            Assert.AreEqual(result.HttpServicePointConnectionLimit, _uut.CrawlBehavior.HttpServicePointConnectionLimit);
            Assert.AreEqual(result.HttpRequestTimeoutInSeconds, _uut.CrawlBehavior.HttpRequestTimeoutInSeconds);
            Assert.AreEqual(result.HttpRequestMaxAutoRedirects, _uut.CrawlBehavior.HttpRequestMaxAutoRedirects);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsHttpRequestAutoRedirectsEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsHttpRequestAutomaticDecompressionEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsSendingCookiesEnabled);
            Assert.AreEqual(false, _uut.CrawlBehavior.IsSslCertificateValidationEnabled);
            Assert.AreEqual(true, _uut.CrawlBehavior.IsRespectUrlNamedAnchorOrHashbangEnabled);
            Assert.AreEqual(result.MinAvailableMemoryRequiredInMb, _uut.CrawlBehavior.MinAvailableMemoryRequiredInMb);
            Assert.AreEqual(result.MaxMemoryUsageInMb, _uut.CrawlBehavior.MaxMemoryUsageInMb);
            Assert.AreEqual(result.MaxMemoryUsageCacheTimeInSeconds, _uut.CrawlBehavior.MaxMemoryUsageCacheTimeInSeconds);
            Assert.AreEqual(result.MaxCrawlDepth, _uut.CrawlBehavior.MaxCrawlDepth);
            Assert.AreEqual(result.MaxLinksPerPage, _uut.CrawlBehavior.MaxLinksPerPage);
            Assert.AreEqual(result.IsForcedLinkParsingEnabled, _uut.CrawlBehavior.IsForcedLinkParsingEnabled);
            Assert.AreEqual(result.MaxRetryCount, _uut.CrawlBehavior.MaxRetryCount);
            Assert.AreEqual(result.MinRetryDelayInMilliseconds, _uut.CrawlBehavior.MinRetryDelayInMilliseconds);

            Assert.AreEqual(result.IsRespectRobotsDotTextEnabled, _uut.Politeness.IsRespectRobotsDotTextEnabled);
            Assert.AreEqual(result.IsRespectMetaRobotsNoFollowEnabled, _uut.Politeness.IsRespectMetaRobotsNoFollowEnabled);
            Assert.AreEqual(result.IsRespectHttpXRobotsTagHeaderNoFollowEnabled, _uut.Politeness.IsRespectHttpXRobotsTagHeaderNoFollowEnabled);
            Assert.AreEqual(result.IsRespectAnchorRelNoFollowEnabled, _uut.Politeness.IsRespectAnchorRelNoFollowEnabled);

            Assert.AreEqual(result.IsIgnoreRobotsDotTextIfRootDisallowedEnabled, _uut.Politeness.IsIgnoreRobotsDotTextIfRootDisallowedEnabled);
            Assert.AreEqual(result.RobotsDotTextUserAgentString, _uut.Politeness.RobotsDotTextUserAgentString);

            Assert.AreEqual(result.MinCrawlDelayPerDomainMilliSeconds, _uut.Politeness.MinCrawlDelayPerDomainMilliSeconds);
            Assert.AreEqual(result.MaxRobotsDotTextCrawlDelayInSeconds, _uut.Politeness.MaxRobotsDotTextCrawlDelayInSeconds);

            Assert.AreEqual(result.IsAlwaysLogin, _uut.Authorization.IsAlwaysLogin);
            Assert.AreEqual(result.LoginPassword, _uut.Authorization.LoginPassword);
            Assert.AreEqual(result.LoginUser, _uut.Authorization.LoginUser);
            Assert.AreEqual(result.UseDefaultCredentials, _uut.Authorization.UseDefaultCredentials);

            Assert.IsNotNull(result.ConfigurationExtensions);
            Assert.AreEqual(result.ConfigurationExtensions["key1"], _uut.ExtensionValues[0].Value);
            Assert.AreEqual(result.ConfigurationExtensions["key2"], _uut.ExtensionValues[1].Value);
        }

        [Test]
        public void SectionHandlerDefaults_MatchPocoDefaults()
        {
            _uut = new AbotConfigurationSectionHandler();
            CrawlConfiguration pocoDefaults = new CrawlConfiguration();

            Assert.AreEqual(pocoDefaults.ConfigurationExtensions.Count, _uut.ExtensionValues.Count);
            Assert.AreEqual(pocoDefaults.CrawlTimeoutSeconds, _uut.CrawlBehavior.CrawlTimeoutSeconds);
            Assert.AreEqual(pocoDefaults.DownloadableContentTypes, _uut.CrawlBehavior.DownloadableContentTypes);
            Assert.AreEqual(pocoDefaults.IsExternalPageCrawlingEnabled, _uut.CrawlBehavior.IsExternalPageCrawlingEnabled);
            Assert.AreEqual(pocoDefaults.IsExternalPageLinksCrawlingEnabled, _uut.CrawlBehavior.IsExternalPageLinksCrawlingEnabled);
            Assert.AreEqual(pocoDefaults.IsRespectRobotsDotTextEnabled, _uut.Politeness.IsRespectRobotsDotTextEnabled);
            Assert.AreEqual(pocoDefaults.IsRespectMetaRobotsNoFollowEnabled, _uut.Politeness.IsRespectMetaRobotsNoFollowEnabled);
            Assert.AreEqual(pocoDefaults.IsRespectHttpXRobotsTagHeaderNoFollowEnabled, _uut.Politeness.IsRespectHttpXRobotsTagHeaderNoFollowEnabled);
            Assert.AreEqual(pocoDefaults.IsRespectAnchorRelNoFollowEnabled, _uut.Politeness.IsRespectAnchorRelNoFollowEnabled);
            Assert.AreEqual(pocoDefaults.IsIgnoreRobotsDotTextIfRootDisallowedEnabled, _uut.Politeness.IsIgnoreRobotsDotTextIfRootDisallowedEnabled);
            Assert.AreEqual(pocoDefaults.IsUriRecrawlingEnabled, _uut.CrawlBehavior.IsUriRecrawlingEnabled);
            Assert.AreEqual(pocoDefaults.MaxConcurrentThreads, _uut.CrawlBehavior.MaxConcurrentThreads);
            Assert.AreEqual(pocoDefaults.MaxRobotsDotTextCrawlDelayInSeconds, _uut.Politeness.MaxRobotsDotTextCrawlDelayInSeconds);
            Assert.AreEqual(pocoDefaults.MaxPagesToCrawl, _uut.CrawlBehavior.MaxPagesToCrawl);
            Assert.AreEqual(pocoDefaults.MaxPagesToCrawlPerDomain, _uut.CrawlBehavior.MaxPagesToCrawlPerDomain);
            Assert.AreEqual(pocoDefaults.MinCrawlDelayPerDomainMilliSeconds, _uut.Politeness.MinCrawlDelayPerDomainMilliSeconds);
            Assert.AreEqual(pocoDefaults.UserAgentString, _uut.CrawlBehavior.UserAgentString);
            Assert.AreEqual(pocoDefaults.RobotsDotTextUserAgentString, _uut.Politeness.RobotsDotTextUserAgentString);
            Assert.AreEqual(pocoDefaults.MaxPageSizeInBytes, _uut.CrawlBehavior.MaxPageSizeInBytes);
            Assert.AreEqual(pocoDefaults.HttpServicePointConnectionLimit, _uut.CrawlBehavior.HttpServicePointConnectionLimit);
            Assert.AreEqual(pocoDefaults.IsSslCertificateValidationEnabled, _uut.CrawlBehavior.IsSslCertificateValidationEnabled);
            Assert.AreEqual(pocoDefaults.HttpRequestTimeoutInSeconds, _uut.CrawlBehavior.HttpRequestTimeoutInSeconds);
            Assert.AreEqual(pocoDefaults.HttpRequestMaxAutoRedirects, _uut.CrawlBehavior.HttpRequestMaxAutoRedirects);
            Assert.AreEqual(pocoDefaults.IsHttpRequestAutoRedirectsEnabled, _uut.CrawlBehavior.IsHttpRequestAutoRedirectsEnabled);
            Assert.AreEqual(pocoDefaults.IsHttpRequestAutomaticDecompressionEnabled, _uut.CrawlBehavior.IsHttpRequestAutomaticDecompressionEnabled);
            Assert.AreEqual(pocoDefaults.IsSendingCookiesEnabled, _uut.CrawlBehavior.IsSendingCookiesEnabled);
            Assert.AreEqual(pocoDefaults.MaxMemoryUsageCacheTimeInSeconds, _uut.CrawlBehavior.MaxMemoryUsageCacheTimeInSeconds);
            Assert.AreEqual(pocoDefaults.MaxMemoryUsageInMb, _uut.CrawlBehavior.MaxMemoryUsageInMb);
            Assert.AreEqual(pocoDefaults.MinAvailableMemoryRequiredInMb, _uut.CrawlBehavior.MinAvailableMemoryRequiredInMb);
            Assert.AreEqual(pocoDefaults.MaxCrawlDepth, _uut.CrawlBehavior.MaxCrawlDepth);
            Assert.AreEqual(pocoDefaults.MaxLinksPerPage, _uut.CrawlBehavior.MaxLinksPerPage);
            Assert.AreEqual(pocoDefaults.IsForcedLinkParsingEnabled, _uut.CrawlBehavior.IsForcedLinkParsingEnabled);
            Assert.AreEqual(pocoDefaults.MaxRetryCount, _uut.CrawlBehavior.MaxRetryCount);
            Assert.AreEqual(pocoDefaults.MinRetryDelayInMilliseconds, _uut.CrawlBehavior.MinRetryDelayInMilliseconds);
        }
    }
}
