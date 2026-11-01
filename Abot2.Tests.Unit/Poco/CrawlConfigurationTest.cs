using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class CrawlConfigurationTest
    {
        [TestMethod]
        public void Constructor_ValidUri_CreatesInstance()
        {
            var unitUnderTest = new CrawlConfiguration();

            Assert.IsNotNull(unitUnderTest.ConfigurationExtensions);
            Assert.IsEmpty(unitUnderTest.ConfigurationExtensions);
            Assert.AreEqual(0, unitUnderTest.CrawlTimeoutSeconds);
            Assert.AreEqual("text/html", unitUnderTest.DownloadableContentTypes);
            Assert.IsFalse(unitUnderTest.IsExternalPageCrawlingEnabled);
            Assert.IsFalse(unitUnderTest.IsExternalPageLinksCrawlingEnabled);
            Assert.IsFalse(unitUnderTest.IsRespectRobotsDotTextEnabled);
            Assert.IsFalse(unitUnderTest.IsRespectMetaRobotsNoFollowEnabled);
            Assert.IsFalse(unitUnderTest.IsRespectAnchorRelNoFollowEnabled);
            Assert.IsFalse(unitUnderTest.IsUriRecrawlingEnabled);
            Assert.AreEqual(10, unitUnderTest.MaxConcurrentThreads);
            Assert.AreEqual(5, unitUnderTest.MaxRobotsDotTextCrawlDelayInSeconds);
            Assert.AreEqual(1000, unitUnderTest.MaxPagesToCrawl);
            Assert.AreEqual(0, unitUnderTest.MaxPagesToCrawlPerDomain);
            Assert.AreEqual(0, unitUnderTest.MinCrawlDelayPerDomainMilliSeconds);
            Assert.AreEqual("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36", unitUnderTest.UserAgentString);
            Assert.AreEqual("abot", unitUnderTest.RobotsDotTextUserAgentString);
            Assert.AreEqual(0, unitUnderTest.MaxPageSizeInBytes);
            Assert.AreEqual(200, unitUnderTest.HttpServicePointConnectionLimit);
            Assert.AreEqual(15, unitUnderTest.HttpRequestTimeoutInSeconds);
            Assert.AreEqual(7, unitUnderTest.HttpRequestMaxAutoRedirects);
            Assert.IsTrue(unitUnderTest.IsHttpRequestAutoRedirectsEnabled);
            Assert.IsFalse(unitUnderTest.IsHttpRequestAutomaticDecompressionEnabled);
            Assert.IsFalse(unitUnderTest.IsSendingCookiesEnabled);
            Assert.IsFalse(unitUnderTest.IsSslCertificateValidationEnabled);
            Assert.IsFalse(unitUnderTest.IsRespectUrlNamedAnchorOrHashbangEnabled);
            Assert.AreEqual(0, unitUnderTest.MaxMemoryUsageCacheTimeInSeconds);
            Assert.AreEqual(0, unitUnderTest.MaxMemoryUsageInMb);
            Assert.AreEqual(0, unitUnderTest.MinAvailableMemoryRequiredInMb);
            Assert.AreEqual(100, unitUnderTest.MaxCrawlDepth);
            Assert.AreEqual(0, unitUnderTest.MaxLinksPerPage);
            Assert.IsFalse(unitUnderTest.IsForcedLinkParsingEnabled);
            Assert.AreEqual(0, unitUnderTest.MaxRetryCount);
            Assert.AreEqual(0, unitUnderTest.MinRetryDelayInMilliseconds);
            Assert.IsNull(unitUnderTest.LoginUser);
            Assert.IsNull(unitUnderTest.LoginPassword);
            Assert.IsFalse(unitUnderTest.IsAlwaysLogin);
            Assert.IsFalse(unitUnderTest.UseDefaultCredentials);
        }


    }
}
