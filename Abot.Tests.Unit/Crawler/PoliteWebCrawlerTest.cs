using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using Abot.Util;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class PoliteWebCrawlerTest
    {
        PoliteWebCrawler _unitUnderTest;
        Mock<IPageRequester> _fakeHttpRequester;
        Mock<IHyperLinkParser> _fakeHyperLinkParser;
        Mock<ICrawlDecisionMaker> _fakeCrawlDecisionMaker;
        Mock<IDomainRateLimiter> _fakeDomainRateLimiter;
        Mock<IMemoryManager> _fakeMemoryManager;
        Mock<IRobotsDotTextFinder> _fakeRobotsDotTextFinder;
        Mock<IRobotsDotText> _fakeRobotsDotText;
        Scheduler _dummyScheduler;
        ManualThreadManager _dummyThreadManager;
        CrawlConfiguration _dummyConfiguration;
        Uri _rootUri;

        [SetUp]
        public void SetUp()
        {
            _fakeHyperLinkParser = new Mock<IHyperLinkParser>();
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

        [Test]
        public void Constructor_Empty()
        {
            Assert.IsNotNull(new PoliteWebCrawler());
        }

        [Test]
        public void Constructor_ZeroMinCrawlDelay_DoesNotThrowExceptionCreatingAnIDomainRateLimiterWithLessThan1Millisec()
        {
            new PoliteWebCrawler(new CrawlConfiguration { MinCrawlDelayPerDomainMilliSeconds = 0 }, null, null, null, null, null, null, null, null);
        }

        [Test]
        public void Crawl_MinCrawlDelayDelayZero_StillCallsDomainRateLimiter()
        {
            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent 
                { 
                    Text = "content here" 
                }
            };
            
            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(1));
        }

        [Test]
        public void Crawl_MinCrawlDelayGreaterThanZero_CallsDomainRateLimiter()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri> { uri1, uri2 };

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            _dummyConfiguration.MinCrawlDelayPerDomainMilliSeconds = 1;//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_ZeroCrawlDelay_StillCallsDomainRateLimiter()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri) 
            { 
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri> { uri1, uri2 };

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(0);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.VerifyAll();
            _fakeHyperLinkParser.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), It.IsAny<long>()), Times.Exactly(0));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_CrawlDelayAboveZero_CallsDomainRateLimiter()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri> { uri1, uri2 };

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(3);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;//BY HAVING A THIS EQUAL TO TRUE WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.VerifyAll();
            _fakeHyperLinkParser.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), 3000), Times.Exactly(1));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_CrawlDelayAboveMinDomainCrawlDelay_CallsDomainRateLimiter()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri> { uri1, uri2 };

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(3);//this is more then the max configured crawl delay (should be ignored)
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;//BY HAVING A THIS EQUAL TO TRUE WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
            _dummyConfiguration.MaxRobotsDotTextCrawlDelayInSeconds = 2; //This is less than the crawl delay (Should Be used)
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.VerifyAll();
            _fakeHyperLinkParser.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), 2000), Times.Exactly(1));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(3));//BY HAVING A CRAWL DELAY ABOVE ZERO WE EXPECT THE IDOMAINRATELIMITER TO BE CALLED
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_PageIsDisallowed_DoesNotCallHttpRequester()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri) 
            { 
                Content = new PageContent
                {
                    Text = "content here" 
                }
            };
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri> { uri1, uri2 };

            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(0);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeDomainRateLimiter.Verify(f => f.AddDomain(It.IsAny<Uri>(), It.IsAny<long>()), Times.Exactly(0));
            _fakeDomainRateLimiter.Verify(f => f.RateLimit(It.IsAny<Uri>()), Times.Exactly(0));
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_PageIsDisallowed_IsIgnoreRobotsDotTextIfRootDisallowedEnabledTrue_CallsHttpRequester()
        {
            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(_rootUri);

            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);
            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision {Allow = true});
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _dummyConfiguration.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeHttpRequester.VerifyAll();
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_RootPageIsAllowed_AllPagesBelowDisallowed_IsIgnoreRobotsDotTextIfRootDisallowedEnabledTrue_CallsHttpRequester()
        {
            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(_rootUri);

            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(_rootUri.AbsoluteUri, It.IsAny<string>())).Returns(true);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(_rootUri.AbsoluteUri + "aaaaa", It.IsAny<string>())).Returns(false);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);
            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _dummyConfiguration.IsIgnoreRobotsDotTextIfRootDisallowedEnabled = true;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.VerifyAll();
            _fakeRobotsDotText.VerifyAll();
            _fakeRobotsDotTextFinder.VerifyAll();
            _fakeHttpRequester.VerifyAll();
        }

        [Test]
        public void Crawl_IsRespectRobotsDotTextTrue_RobotsDotTextFound_UsesCorrectUserAgentString()
        {
            Uri uri1 = new Uri(_rootUri.AbsoluteUri + "a.html");
            Uri uri2 = new Uri(_rootUri.AbsoluteUri + "b.html");

            CrawledPage homePage = new CrawledPage(_rootUri)
            {
                Content = new PageContent
                {
                    Text = "content here"
                }
            };
            CrawledPage page1 = new CrawledPage(uri1);
            CrawledPage page2 = new CrawledPage(uri2);

            List<Uri> links = new List<Uri> { uri1, uri2 };

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            _fakeRobotsDotText.Setup(f => f.GetCrawlDelay(It.IsAny<string>())).Returns(0);
            _fakeRobotsDotText.Setup(f => f.IsUrlAllowed(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(_fakeRobotsDotText.Object);

            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _dummyConfiguration.RobotsDotTextUserAgentString = "abcd";
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _unitUnderTest.Crawl(_rootUri);

            _fakeRobotsDotText.Verify(f => f.GetCrawlDelay(_dummyConfiguration.RobotsDotTextUserAgentString), Times.Exactly(1));
            _fakeRobotsDotText.Verify(f => f.IsUrlAllowed(uri1.AbsoluteUri, _dummyConfiguration.RobotsDotTextUserAgentString), Times.Exactly(1));
            _fakeRobotsDotText.Verify(f => f.IsUrlAllowed(uri1.AbsoluteUri, _dummyConfiguration.RobotsDotTextUserAgentString), Times.Exactly(1));
        }
    }
}
