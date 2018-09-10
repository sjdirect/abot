using Abot.Poco;
using NUnit.Framework;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class CrawlConfigurationTest
    {
        [Test]
        public void Constructor_ValidUri_CreatesInstance()
        {
            CrawlConfiguration unitUnderTest = new CrawlConfiguration();

            Assert.IsNotNull(unitUnderTest.ConfigurationExtensions);
            Assert.AreEqual(0, unitUnderTest.ConfigurationExtensions.Count);
            Assert.AreEqual(0, unitUnderTest.CrawlTimeoutSeconds);
            Assert.AreEqual("text/html", unitUnderTest.DownloadableContentTypes);
            Assert.AreEqual(false, unitUnderTest.IsExternalPageCrawlingEnabled);
            Assert.AreEqual(false, unitUnderTest.IsExternalPageLinksCrawlingEnabled);
            Assert.AreEqual(false, unitUnderTest.IsRespectRobotsDotTextEnabled);
            Assert.AreEqual(false, unitUnderTest.IsRespectMetaRobotsNoFollowEnabled);
            Assert.AreEqual(false, unitUnderTest.IsRespectAnchorRelNoFollowEnabled);
            Assert.AreEqual(false, unitUnderTest.IsUriRecrawlingEnabled);
            Assert.AreEqual(10, unitUnderTest.MaxConcurrentThreads);
            Assert.AreEqual(5, unitUnderTest.MaxRobotsDotTextCrawlDelayInSeconds);
            Assert.AreEqual(1000, unitUnderTest.MaxPagesToCrawl);
            Assert.AreEqual(0, unitUnderTest.MaxPagesToCrawlPerDomain);
            Assert.AreEqual(0, unitUnderTest.MinCrawlDelayPerDomainMilliSeconds);
            Assert.AreEqual("Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko", unitUnderTest.UserAgentString);
            Assert.AreEqual("abot", unitUnderTest.RobotsDotTextUserAgentString);
            Assert.AreEqual(0, unitUnderTest.MaxPageSizeInBytes);
            Assert.AreEqual(200, unitUnderTest.HttpServicePointConnectionLimit);
            Assert.AreEqual(15, unitUnderTest.HttpRequestTimeoutInSeconds);
            Assert.AreEqual(7, unitUnderTest.HttpRequestMaxAutoRedirects);
            Assert.AreEqual(true, unitUnderTest.IsHttpRequestAutoRedirectsEnabled);
            Assert.AreEqual(false, unitUnderTest.IsHttpRequestAutomaticDecompressionEnabled);
            Assert.AreEqual(false, unitUnderTest.IsSendingCookiesEnabled);
            Assert.AreEqual(true, unitUnderTest.IsSslCertificateValidationEnabled);
            Assert.AreEqual(false, unitUnderTest.IsRespectUrlNamedAnchorOrHashbangEnabled);
            Assert.AreEqual(0, unitUnderTest.MaxMemoryUsageCacheTimeInSeconds);
            Assert.AreEqual(0, unitUnderTest.MaxMemoryUsageInMb);
            Assert.AreEqual(0, unitUnderTest.MinAvailableMemoryRequiredInMb);
            Assert.AreEqual(100, unitUnderTest.MaxCrawlDepth);
            Assert.AreEqual(0, unitUnderTest.MaxLinksPerPage);
            Assert.AreEqual(false, unitUnderTest.IsForcedLinkParsingEnabled);
            Assert.AreEqual(0, unitUnderTest.MaxRetryCount);
            Assert.AreEqual(0, unitUnderTest.MinRetryDelayInMilliseconds);
            Assert.AreEqual(null, unitUnderTest.LoginUser);
            Assert.AreEqual(null, unitUnderTest.LoginPassword);
            Assert.AreEqual(false, unitUnderTest.IsAlwaysLogin);
            Assert.AreEqual(false, unitUnderTest.UseDefaultCredentials);
        }


    }
}
