using Abot.Core;
using Abot.Poco;
using HtmlAgilityPack;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class CrawlDecisionMakerTest
    {
        CrawlDecisionMaker _unitUnderTest;
        CrawlContext _crawlContext;
        Mock<IScheduler> _fakeScheduler;

        [SetUp]
        public void SetUp()
        {
            _fakeScheduler = new Mock<IScheduler>();
            _crawlContext = new CrawlContext();
            _crawlContext.CrawlConfiguration = new CrawlConfiguration { UserAgentString = "aaa" };
            _crawlContext.Scheduler = _fakeScheduler.Object;
            _unitUnderTest = new CrawlDecisionMaker();
        }


        [Test]
        public void ShouldCrawlPage_NullPageToCrawl_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(null, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null page to crawl", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldCrawlPage_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldCrawlPage_NonDuplicate_ReturnsTrue()
        {
            _crawlContext.CrawlStartDate = DateTime.Now;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_NonHttpOrHttpsSchemes_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("file:///C:/Users/")), _crawlContext);
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

        [Test]
        public void ShouldCrawlPage_OverMaxPageToCrawlLimit_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxPagesToCrawl = 100;
            _crawlContext.CrawledCount = 100;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/b")) { IsInternal = true }, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawl limit of [100] has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldCrawlPage_OverMaxPageToCrawlLimitByScheduler_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxPagesToCrawl = 100;
            _crawlContext.CrawledCount = 1;

            _fakeScheduler.Setup(f => f.Count).Returns(100);

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/b")) { IsInternal = true }, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxPagesToCrawl limit of [100] has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldCrawlPage_ZeroMaxPageToCrawlLimit_ReturnsTrue()
        {
            CrawlContext crawlContext = new CrawlContext
            {
                CrawlConfiguration = new CrawlConfiguration
                {
                    MaxPagesToCrawl = 0
                },
                CrawledCount = 100
            };

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(new PageToCrawl(new Uri("http://a.com/")) { IsInternal = true }, crawlContext);

            Assert.IsTrue(result.Allow);
        }

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledFalse_PageIsExternal_ReturnsFalse()
        {
            _crawlContext.CrawlStartDate = DateTime.Now.AddSeconds(-100);
            _crawlContext.CrawlConfiguration.CrawlTimeoutSeconds = 0;
            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledTrue_PageIsExternal_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled = true;
            _crawlContext.CrawlStartDate = DateTime.Now;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledFalse_PageIsInternal_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled = false;
            _crawlContext.CrawlStartDate = DateTime.Now;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_IsExternalPageCrawlingEnabledTrue_PageIsInternal_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.IsExternalPageCrawlingEnabled = true;
            _crawlContext.CrawlStartDate = DateTime.Now;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_OverMaxPagesToCrawlPerDomain_ReturnsFalse()
        {
            Uri uri = new Uri("http://a.com/");

            ConcurrentDictionary<string,int> countByDomain = new ConcurrentDictionary<string,int>();
            countByDomain.TryAdd(uri.Authority, 100);

            _crawlContext.CrawlStartDate = DateTime.Now;
            _crawlContext.CrawlCountByDomain = countByDomain;
            _crawlContext.CrawlConfiguration.MaxPagesToCrawlPerDomain = 100;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_OverMaxPagesToCrawlPerDomain_IsRetry_ReturnsTrue()
        {
            Uri uri = new Uri("http://a.com/");
            CrawlConfiguration config = new CrawlConfiguration
            {
                MaxPagesToCrawlPerDomain = 100
            };
            ConcurrentDictionary<string, int> countByDomain = new ConcurrentDictionary<string, int>();
            countByDomain.TryAdd(uri.Authority, 100);
            CrawlContext crawlContext = new CrawlContext
            {
                CrawlConfiguration = config,
                CrawlStartDate = DateTime.Now,
                CrawlCountByDomain = countByDomain
            };

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_OverMaxCrawlDepth_ReturnsFalse()
        {
            CrawlContext crawlContext = new CrawlContext
            {
                CrawlConfiguration = new CrawlConfiguration
                {
                    MaxCrawlDepth = 2
                }
            };

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_EqualToMaxCrawlDepth_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.MaxCrawlDepth = 2;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_RedirectChainOverMax_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects = 7;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_RedirectChainUnderMax_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects = 7;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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

        [Test]
        public void ShouldCrawlPage_RedirectChainEqualToMax_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.HttpRequestMaxAutoRedirects = 7;

            CrawlDecision result = _unitUnderTest.ShouldCrawlPage(
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


        [Test]
        public void ShouldCrawlPageLinks_NullCrawledPage_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(null, new CrawlContext());

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldCrawlPageLinks_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/a.html"))
                {
                    Content = new PageContent
                    {
                        Text = "aaaa"
                    }
                }, null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldCrawlPageLinks_NullHtmlContent_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/"))
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

        [Test]
        public void ShouldCrawlPageLinks_WhitespaceHtmlContent_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/"))
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

        [Test]
        public void ShouldCrawlPageLinks_EmptyHtmlContent_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(new CrawledPage(new Uri("http://a.com/"))
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

        [Test]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledFalse_ExternalLink_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
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

        [Test]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledTrue_InternalLink_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
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

        [Test]
        public void ShouldCrawlPageLinks_IsExternalPageLinksCrawlingEnabledFalse_InternalLink_ReturnsTrue()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
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

        [Test]
        public void ShouldCrawlPageLinks_IsEqualToMaxCrawlDepth_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
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

        [Test]
        public void ShouldCrawlPageLinks_IsAboveMaxCrawlDepth_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldCrawlPageLinks(
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


        [Test]
        public void ShouldDownloadPageContent_DownloadablePage_ReturnsTrue()
        {
            Uri valid200StatusUri = new Uri("http://localhost.fiddler:1111/");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(new CrawlConfiguration { UserAgentString = "aaa" }).MakeRequest(valid200StatusUri), _crawlContext);

            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_NullCrawledPage_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(null, new CrawlContext());
            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new CrawledPage(new Uri("http://a.com/a.html")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_NullHttpWebResponse_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(
                new CrawledPage(new Uri("http://b.com/a.html"))
                {
                    HttpWebResponse = null
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

        [Test]
        public void ShouldDownloadPageContent_HttpStatusNon200_ReturnsFalse()
        {
            Uri non200Uri = new Uri("http://localhost.fiddler:1111/HttpResponse/Status403");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(_crawlContext.CrawlConfiguration).MakeRequest(non200Uri), new CrawlContext());

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("HttpStatusCode is not 200", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_NonHtmlPage_ReturnsFalse()
        {
            Uri imageUrl = new Uri("http://localhost.fiddler:1111/Content/themes/base/images/ui-bg_flat_0_aaaaaa_40x100.png");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(_crawlContext.CrawlConfiguration).MakeRequest(imageUrl), _crawlContext);

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Content type is not any of the following: text/html", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_NonHtmlPage_DownloadableContentTypesWithSpaces_ReturnsFalse()
        {
            Uri imageUrl = new Uri("http://localhost.fiddler:1111/Content/themes/base/images/ui-bg_flat_0_aaaaaa_40x100.png");//Content type fo this link is image/png

            _crawlContext.CrawlConfiguration.DownloadableContentTypes = "text/hmtl, ,    , application/pdf";
            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(_crawlContext.CrawlConfiguration).MakeRequest(imageUrl), _crawlContext);

            Assert.AreEqual(false, result.Allow);
            Assert.AreEqual("Content type is not any of the following: text/hmtl,application/pdf", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_DownloadableContenttypesWithSpaces_TrimsSpaces_ReturnsTrue()
        {
            Uri valid200StatusUri = new Uri("http://localhost.fiddler:1111/");//Content type fo this link is text/html
            CrawlConfiguration crawlConfiguration = new CrawlConfiguration
            {
                UserAgentString = "aaa",
                DownloadableContentTypes = "text/html  , application/pdf, ,somethingelse"
            };
            _crawlContext.CrawlConfiguration = crawlConfiguration;

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(crawlConfiguration).MakeRequest(valid200StatusUri), _crawlContext);

            Assert.AreEqual(true, result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldDownloadPageContent_AboveMaxPageSize_ReturnsFalse()
        {
            Uri valid200StatusUri = new Uri("http://localhost.fiddler:1111/");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(_crawlContext.CrawlConfiguration).MakeRequest(valid200StatusUri), 
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

        [Test]
        public void ShouldDownloadPageContent_MaxPageSizeInBytesZero_ReturnsTrue()
        {
            Uri valid200StatusUri = new Uri("http://localhost.fiddler:1111/");

            CrawlDecision result = _unitUnderTest.ShouldDownloadPageContent(new PageRequester(_crawlContext.CrawlConfiguration).MakeRequest(valid200StatusUri),
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


        [Test]
        public void ShouldRecrawlPage_RetryablePage_ReturnsTrue()
        {
            _crawlContext.CrawlConfiguration.MaxRetryCount = 5;

            CrawlDecision result = _unitUnderTest.ShouldRecrawlPage(
                new CrawledPage(new Uri("http://a.com/"))
                {
                    WebException = new WebException("something bad"),
                    RetryCount = 1
                },
                _crawlContext);

            Assert.IsTrue(result.Allow);
            Assert.AreEqual("", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }


        [Test]
        public void ShouldRecrawlPage_NullPageToCrawl_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldRecrawlPage(null, _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawled page", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldRecrawlPage_NullCrawlContext_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldRecrawlPage(new CrawledPage(new Uri("http://a.com/")), null);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("Null crawl context", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);
        }

        [Test]
        public void ShouldRecrawlPage_NullWebException_ReturnsFalse()
        {
            CrawlDecision result = _unitUnderTest.ShouldRecrawlPage(
                new CrawledPage(new Uri("http://a.com/"))
                {
                    WebException = null
                },
                _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("WebException did not occur", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }

        [Test]
        public void ShouldRecrawlPage_MaxRetryCountBelow1_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxRetryCount = 0;

            CrawlDecision result = _unitUnderTest.ShouldRecrawlPage(
                new CrawledPage(new Uri("http://a.com/"))
                {
                    WebException = new WebException("something bad")
                },
                _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxRetryCount is less than 1", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }

        [Test]
        public void ShouldRecrawlPage_MaxRetryCountBelowAboveMax_ReturnsFalse()
        {
            _crawlContext.CrawlConfiguration.MaxRetryCount = 5;

            CrawlDecision result = _unitUnderTest.ShouldRecrawlPage(
                new CrawledPage(new Uri("http://a.com/"))
                {
                    WebException = new WebException("something bad"),
                    RetryCount = 5
                },
                _crawlContext);

            Assert.IsFalse(result.Allow);
            Assert.AreEqual("MaxRetryCount has been reached", result.Reason);
            Assert.IsFalse(result.ShouldHardStopCrawl);
            Assert.IsFalse(result.ShouldStopCrawl);

        }

        private HtmlDocument GetHtmlDocument(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }
    }
}
