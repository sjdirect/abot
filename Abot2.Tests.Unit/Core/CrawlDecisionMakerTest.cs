using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class CrawlDecisionMakerTest
    {
        CrawlDecisionMaker _unitUnderTest;
        CrawlContext _crawlContext;
        CrawledPage _validCrawledPage;
        Mock<IScheduler> _fakeScheduler;

        [TestInitialize]
        public void SetUp()
        {
            _fakeScheduler = new Mock<IScheduler>();
            _crawlContext = new CrawlContext();
            _crawlContext.CrawlConfiguration = new CrawlConfiguration { UserAgentString = "aaa" };
            _crawlContext.Scheduler = _fakeScheduler.Object;
            _unitUnderTest = new CrawlDecisionMaker();

            _validCrawledPage = GetValidCrawledPage();
        }


        [TestMethod]
        public void ShouldCrawlPage_NullPageToCrawl_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPage(null, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null page to crawl", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_NullCrawlContext_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_NonDuplicate_ReturnsTrue()
        {
            _crawlContext.CrawlStartDate = DateTime.Now;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }

        [TestMethod]
        public void ShouldCrawlPage_NonHttpOrHttpsSchemes_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("file:///C:/Users/")), _crawlContext);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("mailto:user@yourdomainname.com")), _crawlContext);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("ftp://user@yourdomainname.com")), _crawlContext);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("callto:+1234567")), _crawlContext);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

            result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("tel:+1234567")), _crawlContext);
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Scheme does not begin with http", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_OverMaxPageToCrawlLimit_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxPagesToCrawl = 100;
            _crawlContext.CrawledCount = 100;

            var result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/b")) { IsInternal = true }, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawl limit of [100] has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_OverMaxPageToCrawlLimitByScheduler_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxPagesToCrawl = 100;
            _crawlContext.CrawledCount = 1;

            _fakeScheduler.Setup(f => f.Count).Returns(100);

            var result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/b")) { IsInternal = true }, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawl limit of [100] has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_ZeroMaxPageToCrawlLimit_ReturnsTrue()
        {
            var crawlContext = new CrawlContext
            {
                CrawlConfiguration = new CrawlConfiguration
                {
                    MaxPagesToCrawl = 0
                },
                CrawledCount = 100
            };

            var result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/")) { IsInternal = true }, crawlContext);

            Assert.IsTrue(result.Allow);
        }

        [TestMethod]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledFalse_PageIsExternal_ReturnsFalse()
        {
            _crawlContext.CrawlStartDate = DateTime.Now.AddSeconds(-100);
            _crawlContext.CrawlConfiguration.CrawlTimeoutSeconds = 0;
            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = false
                }, 
                _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Link is external", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledTrue_PageIsExternal_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled = true;
            _crawlContext.CrawlStartDate = DateTime.Now;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = false
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledFalse_PageIsInternal_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled = false;
            _crawlContext.CrawlStartDate = DateTime.Now;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledTrue_PageIsInternal_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled = true;
            _crawlContext.CrawlStartDate = DateTime.Now;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_OverMaxPagesToCrawlPerDomain_ReturnsFalse()
        {
            var uri = new Uri("http://a.com/");

            var countByDomain = new ConcurrentDictionary<string,int>();
            countByDomain.TryAdd(uri.Authority, 100);

            _crawlContext.CrawlStartDate = DateTime.Now;
            _crawlContext.CrawlCountByDomain = countByDomain;
            _crawlContext.CrawlConfiguration.MaxPagesToCrawlPerDomain = 100;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri(uri.AbsoluteUri + "anotherpage"))
                {
                    IsInternal = true
                },
                _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawlPerDomain limit of [100] has been reached for domain [a.com]", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_OverMaxPagesToCrawlPerDomain_IsRetry_ReturnsTrue()
        {
            var uri = new Uri("http://a.com/");
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawlPerDomain = 100
            };
            var countByDomain = new ConcurrentDictionary<string, int>();
            countByDomain.TryAdd(uri.Authority, 100);
            var crawlContext = new CrawlContext
            {
                CrawlConfiguration = config,
                CrawlStartDate = DateTime.Now,
                CrawlCountByDomain = countByDomain
            };

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri(uri.AbsoluteUri + "anotherpage"))
                {
                    IsRetry = true,
                    IsInternal = true
                },
                crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_OverMaxCrawlDepth_ReturnsFalse()
        {
            var crawlContext = new CrawlContext
            {
                CrawlConfiguration = new CrawlConfiguration
                {
                    MaxCrawlDepth = 2
                }
            };

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true,
                    CrawlDepth = 3
                },
                crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Crawl depth is above max", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_EqualToMaxCrawlDepth_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.MaxCrawlDepth = 2;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true,
                    CrawlDepth = 2
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_RedirectChainOverMax_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects = 7;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true,
                    RedirectedFrom = new CrawledPage(new Uri("http://Doesntmatter.com")),
                    RedirectPosition = 8
                },
                _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("HttpRequestMaxAutoRedirects limit of [7] has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_RedirectChainUnderMax_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects = 7;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true,
                    RedirectedFrom = new CrawledPage(new Uri("http://Doesntmatter.com")),
                    RedirectPosition = 6
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPage_RedirectChainEqualToMax_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects = 7;

            var result = _unitUnderTest.ShouldCrawlPage(
                new PageToCrawl(new Uri("http://a.com/"))
                {
                    IsInternal = true,
                    RedirectedFrom = new CrawledPage(new Uri("http://Doesntmatter.com")),
                    RedirectPosition = 7
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }


        [TestMethod]
        public void ShouldCrawlPageLinks_NullCrawledPage_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(null, new CrawlContext());

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_NullCrawlContext_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(_validCrawledPage, null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_NullHtmlContent_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/"))
                {
                    Content = new PageContent
                    {
                        Text = null
                    }
                }, new CrawlContext());
            
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Page has no content", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_WhitespaceHtmlContent_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/"))
                {
                    Content = new PageContent
                    {
                        Text = "     "
                    }
                }, new CrawlContext());

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Page has no content", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_EmptyHtmlContent_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/"))
                {
                    Content = new PageContent
                    {
                        Text = ""
                    }
                }, new CrawlContext());
            
            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Page has no content", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledFalse_ExternalLink_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    Content = new PageContent
                    {
                        Text = "aaaa"
                    },
                    IsInternal = false
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageLinksCrawlingEnabled = false
                    }
                });
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Link is external", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledTrue_InternalLink_ReturnsTrue()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    Content = new PageContent
                    {
                        Text = "aaaa"
                    },
                    IsInternal = true
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageLinksCrawlingEnabled = true
                    }
                });
            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledFalse_InternalLink_ReturnsTrue()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    Content = new PageContent
                    {
                        Text = "aaaa"
                    },
                    IsInternal = true
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        IsExternalPageLinksCrawlingEnabled = false
                    }
                });
            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_IsEqualToMaxCrawlDepth_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    Content = new PageContent
                    {
                        Text = "aaaa"
                    },
                    IsInternal = true,
                    CrawlDepth = 2
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        MaxCrawlDepth = 2
                    }
                });
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Crawl depth is above max", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldCrawlPageLinks_IsAboveMaxCrawlDepth_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldCrawlPageLinks(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    Content = new PageContent
                    {
                        Text = "aaaa"
                    },
                    IsInternal = true,
                    CrawlDepth = 3
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ "),
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        MaxCrawlDepth = 2
                    }
                });
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Crawl depth is above max", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }


        [TestMethod]
        public void ShouldDownloadPageContent_DownloadablePage_ReturnsTrue()
        {
            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage, _crawlContext);

            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_NullCrawledPage_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldDownloadPageContent(null, new CrawlContext());
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_NullCrawlContext_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldDownloadPageContent(new CrawledPage(new Uri("http://a.com/a.html")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_NullHttpWebResponse_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldDownloadPageContent(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    HttpResponseMessage = null
                },
                new CrawlContext
                {
                    RootUri = new Uri("http://a.com/ ")
                });
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Null HttpWebResponse", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_HttpStatusNon200_ReturnsFalse()
        {
            _validCrawledPage.HttpResponseMessage.StatusCode = HttpStatusCode.Forbidden;

            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage, _crawlContext);

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("HttpStatusCode is not 200", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_NonHtmlPage_ReturnsFalse()
        {
            _validCrawledPage.HttpResponseMessage.Content = new StringContent("aaa", Encoding.UTF8, "image/jpg");

            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage, _crawlContext);

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Content type is not any of the following: text/html", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_NonHtmlPage_DownloadableContentTypesWithSpaces_ReturnsFalse()
        {
            _validCrawledPage.HttpResponseMessage.Content = new StringContent("aaa", Encoding.UTF8, "image/jpg");

            _crawlContext.CrawlConfiguration.DownloadableContentTypes = "text/hmtl, ,    , application/pdf";
            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage, _crawlContext);

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Content type is not any of the following: text/hmtl,application/pdf", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_DownloadableContenttypesWithSpaces_TrimsSpaces_ReturnsTrue()
        {
            var crawlConfiguration = new CrawlConfiguration
            {
                UserAgentString = "aaa",
                DownloadableContentTypes = "text/html  , application/pdf, ,somethingelse"
            };
            _crawlContext.CrawlConfiguration = crawlConfiguration;

            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage, _crawlContext);

            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_AboveMaxPageSize_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage,
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        MaxPageSizeInBytes = 5
                    }
                });

            Assert.IsFalse(result.Allow);
            //Assert.AreEqual("Page size of [1298] bytes is above the max allowable of [5] bytes", result.Reason); //This fluctuates depending on the .net version runtime installed
            Assert.IsTrue(result.Reason.StartsWith("Page size of ["));
            Assert.IsTrue(result.Reason.EndsWith("] bytes is above the max allowable of [5] bytes"));
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldDownloadPageContent_MaxPageSizeInBytesZero_ReturnsTrue()
        {
            var result = _unitUnderTest.ShouldDownloadPageContent(_validCrawledPage,
                new CrawlContext
                {
                    CrawlConfiguration = new CrawlConfiguration
                    {
                        MaxPageSizeInBytes = 0
                    }
                });

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }


        [TestMethod]
        public void ShouldRecrawlPage_RetryablePage_ReturnsTrue()
        {
            _validCrawledPage.HttpRequestException = new HttpRequestException("Oh no");
            _validCrawledPage.RetryCount = 1;
            _crawlContext.CrawlConfiguration.MaxRetryCount = 5;

            var result = _unitUnderTest.ShouldRecrawlPage(_validCrawledPage, _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }


        [TestMethod]
        public void ShouldRecrawlPage_NullPageToCrawl_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldRecrawlPage(null, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldRecrawlPage_NullCrawlContext_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldRecrawlPage(new CrawledPage(new Uri("http://a.com/")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [TestMethod]
        public void ShouldRecrawlPage_NullWebException_ReturnsFalse()
        {
            var result = _unitUnderTest.ShouldRecrawlPage(_validCrawledPage, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("WebException did not occur", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }

        [TestMethod]
        public void ShouldRecrawlPage_MaxRetryCountBelow1_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxRetryCount = 0;
            _validCrawledPage.HttpRequestException = new HttpRequestException("something bad");
            var result = _unitUnderTest.ShouldRecrawlPage(_validCrawledPage, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxRetryCount is less than 1", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }

        [TestMethod]
        public void ShouldRecrawlPage_MaxRetryCountBelowAboveMax_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxRetryCount = 5;
            _validCrawledPage.HttpRequestException = new HttpRequestException("something bad");
            _validCrawledPage.RetryCount = 5;

            var result = _unitUnderTest.ShouldRecrawlPage(_validCrawledPage, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxRetryCount has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }


        private CrawledPage GetValidCrawledPage()
        {
            var valid200StatusUri = new Uri("http://localhost.fiddler:1111/");
            var crawledPage = new CrawledPage(valid200StatusUri)
            {
                Content = GetValidContent(),
                HttpResponseMessage = GetValidHttpResponseMessage()
            };

            return crawledPage;
        }

        private PageContent GetValidContent()
        {
            return new PageContent()
            {
                Encoding = Encoding.UTF8,
                Text = "AAAAA",
                Charset = "whatever",
                Bytes = Encoding.UTF8.GetBytes("AAAAA")
            };
        }

        private HttpResponseMessage GetValidHttpResponseMessage()
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Doesn't matter", Encoding.UTF8, "text/html")
            };
        }

    }
}
