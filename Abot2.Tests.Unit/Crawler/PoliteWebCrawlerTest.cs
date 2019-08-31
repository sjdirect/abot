using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class PoliteWebCrawlerTest
    {
        PoliteWebCrawler _unitUnderTest;
        Mock<IPageRequester> _fakeHttpRequester;
        Mock<IHtmlParser> _fakeHtmlParser;
        Mock<ICrawlDecisionMaker> _fakeCrawlDecisionMaker;
        Mock<IDomainRateLimiter> _fakeDomainRateLimiter;
        Mock<IMemoryManager> _fakeMemoryManager;
        Mock<IRobotsDotTextFinder> _fakeRobotsDotTextFinder;
        Mock<IRobotsDotText> _fakeRobotsDotText;
        Scheduler _dummyScheduler;
        ManualThreadManager _dummyThreadManager;
        CrawlConfiguration _dummyConfiguration;
        Uri _rootUri;

        [TestInitialize]
        public void SetUp()
        {
            _fakeHtmlParser = new Mock<IHtmlParser>();
            _fakeHttpRequester = new Mock<IPageRequester>();
            _fakeCrawlDecisionMaker = new Mock<ICrawlDecisionMaker>();
            _fakeDomainRateLimiter = new Mock<IDomainRateLimiter>();
            _fakeMemoryManager = new Mock<IMemoryManager>();
            _fakeRobotsDotTextFinder = new Mock<IRobotsDotTextFinder>();
            _fakeRobotsDotText = new Mock<IRobotsDotText>();

            _dummyScheduler = new Scheduler();
            _dummyThreadManager = new ManualThreadManager(1);
            _dummyConfiguration = new CrawlConfiguration();
            _dummyConfiguration.ConfigurationExtensions.Add("somekey", "someval");

            _rootUri = new Uri("http://a.com/");
        }

        [TestMethod]
        public void Constructor_Empty()
        {
            Assert.IsNotNull(new PoliteWebCrawler());
        }

        [TestMethod]
        public void Constructor_ZeroMinCrawlDelay_DoesNotThrowExceptionCreatingAnIDomainRateLimiterWithLessThan1Millisec()
        {
            using (var unused = new PoliteWebCrawler(new CrawlConfiguration {MinCrawlDelayPerDomainMilliSeconds = 0},
                null, null, null, null, null, null, null, null))
            {

            }
        }

        [TestMethod]
        public async Task Crawl_MinCrawlDelayDelayZero_StillCallsDomainRateLimiter()
        {
            var homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent 
                { 
                    Text = "content here" 
                }
            };
            
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(1));
        }

        [TestMethod]
        public async Task Crawl_MinCrawlDelayGreaterThanZero_CallsDomainRateLimiter()
        {
            var uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            var uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            var homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            var page1 = new CrawledPage(uri1);
            var page2 = new CrawledPage(uri2);

            var links = new List<HyperLink>
            {
                new HyperLink(){HrefValue = uri1},
                new HyperLink(){HrefValue = uri2}
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            _dummyConfiguration.MinCrawlDelayPerDomainMilliSeconds = 1;//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_ZeroCrawlDelay_StillCallsDomainRateLimiter()
        {
            var uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            var uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            var homePage = new CrawledPage(_rootUri) 
            { 
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            var page1 = new CrawledPage(uri1);
            var page2 = new CrawledPage(uri2);

            var links = new List<HyperLink>
            {
                new HyperLink(){HrefValue = uri1},
                new HyperLink(){HrefValue = uri2}
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(0);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.VerifyAll();
            _fakeHtmlParser.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), It.IsAny<long>()), Times.Exactly(0));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_CrawlDelayAboveZero_CallsDomainRateLimiter()
        {
            var uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            var uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            var homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            var page1 = new CrawledPage(uri1);
            var page2 = new CrawledPage(uri2);

            var links = new List<HyperLink>
            {
                new HyperLink(){HrefValue = uri1},
                new HyperLink(){HrefValue = uri2}
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(3);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;//BY HAVING A THIS EQUAL TO TRUE WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.VerifyAll();
            _fakeHtmlParser.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), 3000), Times.Exactly(1));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_CrawlDelayAboveMinDomainCrawlDelay_CallsDomainRateLimiter()
        {
            var uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            var uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            var homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            var page1 = new CrawledPage(uri1);
            var page2 = new CrawledPage(uri2);

            var links = new List<HyperLink>
            {
                new HyperLink(){HrefValue = uri1},
                new HyperLink(){HrefValue = uri2}
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(3);//this is more then the max configured crawl delay (should be ignored)
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;//BY HAVING A THIS EQUAL TO TRUE WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
            _dummyConfiguration.MaxRobotsDotTextCrawlDelayInSeconds = 2; //This is less than the crawl delay (Should Be used)
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.VerifyAll();
            _fakeHtmlParser.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), 2000), Times.Exactly(1));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_PageIsDisallowed_DoesNotCallHttpRequester()
        {
            var homePage = new CrawledPage(_rootUri) 
            { 
                Content = new PageContent
                {
                    Text = "content here" 
                }
            };

            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(0);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), It.IsAny<long>()), Times.Exactly(0));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(0));
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_PageIsDisallowed_IsIgnoreRobotsDotTextIfRootDisallowedEnabledTrue_CallsHttpRequester()
        {
            var page1 = new CrawledPage(_rootUri);

            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision {Allow = true});
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _dummyConfiguration.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeHttpRequester.VerifyAll();
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_RootPageIsAllowed_AllPagesBelowDisallowed_IsIgnoreRobotsDotTextIfRootDisallowedEnabledTrue_CallsHttpRequester()
        {
            var page1 = new CrawledPage(_rootUri);

            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(_rootUri.AbsoluteUri, It.IsAny<string>())).Returns(true);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(_rootUri.AbsoluteUri + "aaaaa", It.IsAny<string>())).Returns(false);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _dummyConfiguration.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeHttpRequester.VerifyAll();
        }

        [TestMethod]
        public async Task Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_UsesCorrectUserAgentString()
        {
            var uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            var uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            var homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            var page1 = new CrawledPage(uri1);
            var page2 = new CrawledPage(uri2);

            var links = new List<HyperLink>
            {
                new HyperLink(){HrefValue = uri1},
                new HyperLink(){HrefValue = uri2}
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(0);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(_fakeRobotsDotText.Object));

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _dummyConfiguration.RobotsDotTextUserAgentString = "abcd";
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeRobotsDotText.Verify(f => f.GetCrawlDelay(_dummyConfiguration.RobotsDotTextUserAgentString), Times.Exactly(1));
            _fakeRobotsDotText.Verify(f => f.IsUrlAllowed(uri1.AbsoluteUri, _dummyConfiguration.RobotsDotTextUserAgentString), Times.Exactly(1));
            _fakeRobotsDotText.Verify(f => f.IsUrlAllowed(uri1.AbsoluteUri, _dummyConfiguration.RobotsDotTextUserAgentString), Times.Exactly(1));
        }
    }
}
