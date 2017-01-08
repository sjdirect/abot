using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using Abot.Util;
using HtmlAgilityPack;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class WebCrawlerTest
    {
        IPoliteWebCrawler _unitUnderTest;
        Mock<IPageRequester> _fakeHttpRequester;
        Mock<IHyperLinkParser> _fakeHyperLinkParser;
        Mock<ICrawlDecisionMaker> _fakeCrawlDecisionMaker;
        Mock<IMemoryManager> _fakeMemoryManager;
        Mock<IDomainRateLimiter> _fakeDomainRateLimiter;
        Mock<IRobotsDotTextFinder> _fakeRobotsDotTextFinder;
        
        Scheduler _dummyScheduler;
        TaskThreadManager _dummyThreadManager;
        CrawlConfiguration _dummyConfiguration;
        Uri _rootUri;

        [SetUp]
        public void SetUp()
        {
            _fakeHyperLinkParser = new Mock<IHyperLinkParser>();
            _fakeHttpRequester = new Mock<IPageRequester>();
            _fakeCrawlDecisionMaker = new Mock<ICrawlDecisionMaker>();
            _fakeMemoryManager = new Mock<IMemoryManager>();
            _fakeDomainRateLimiter = new Mock<IDomainRateLimiter>();
            _fakeRobotsDotTextFinder = new Mock<IRobotsDotTextFinder>();


            _dummyScheduler = new Scheduler();
            _dummyThreadManager = new TaskThreadManager(10);
            _dummyConfiguration = new CrawlConfiguration();
            _dummyConfiguration.ConfigurationExtensions.Add("somekey", "someval");

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _unitUnderTest.CrawlBag.SomeVal = "SomeVal";
            _unitUnderTest.CrawlBag.SomeList = new List<string>() { "a", "b" };
            _rootUri = new Uri("http://a.com/");
        }

        [Test]
        public void Constructor_LoadsConfigFromFile()
        {
            new PoliteWebCrawler();
        }

        [Test]
        public void Constructor_ConfigValueMaxConcurrentThreadsIsZero_DoesNotThrowException()
        {
            _dummyConfiguration.MaxConcurrentThreads = 0;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, null, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
        }

        [Test]
        public void Crawl_CallsDependencies()
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

            List<Uri> links = new List<Uri>{uri1, uri2};

            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{Allow = true});
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{ Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequest(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequest(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri)), Times.Exactly(1));
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == uri1)), Times.Exactly(1));
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == uri2)), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
        }


        [Test]
        public void Crawl_NullUri_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => _unitUnderTest.Crawl(null));
        }

        [Test]
        public void Crawl_ExceptionThrownByScheduler_SetsCrawlResultError()
        {
            Mock<IScheduler> fakeScheduler = new Mock<IScheduler>();
            Exception ex = new Exception("oh no");
            fakeScheduler.Setup(f => f.Count).Throws(ex);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, fakeScheduler.Object, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            
            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            fakeScheduler.Verify(f => f.Count, Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
        }

        [Test]
        public void Crawl_ExceptionThrownByFirstShouldSchedulePageLink_SetsCrawlResultError()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            Exception ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Throws(ex);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [Test]
        public void Crawl_SingleThread_ExceptionThrownDuringProcessPage_SetsCrawlResultError()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            Exception ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Throws(ex);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }


        [Test]
        public void Crawl_MultiThread_ExceptionThrownDuringProcessPage_SetsCrawlResultError()
        {
            Exception ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Throws(ex);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        #region Synchronous Event Tests

        [Test]
        public void Crawl_CrawlDecisionMakerMethodsReturnTrue_PageCrawlStartingAndCompletedEventsFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
        }

        [Test]
        public void Crawl_CrawlDecisionMakerShouldCrawlLinksMethodReturnsFalse_PageLinksCrawlDisallowedEventFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(1, _pageLinksCrawlDisallowedCount);
        }

        [Test]
        public void Crawl_CrawlDecisionMakerShouldCrawlMethodReturnsFalse_PageCrawlDisallowedEventFires()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Never());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Never());

            Assert.AreEqual(0, _pageCrawlStartingCount);
            Assert.AreEqual(0, _pageCrawlCompletedCount);
            Assert.AreEqual(1, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
        }


        [Test]
        public void Crawl_PageCrawlStartingAndCompletedEventSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlStarting += new EventHandler<PageCrawlStartingArgs>(ThrowExceptionWhen_PageCrawlStarting);
            _unitUnderTest.PageCrawlCompleted += new EventHandler<PageCrawlCompletedArgs>(ThrowExceptionWhen_PageCrawlCompleted);
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }

        [Test]
        public void Crawl_PageCrawlDisallowedSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;
            _unitUnderTest.PageCrawlDisallowed += new EventHandler<PageCrawlDisallowedArgs>(ThrowExceptionWhen_PageCrawlDisallowed);
            _unitUnderTest.PageLinksCrawlDisallowed += new EventHandler<PageLinksCrawlDisallowedArgs>(ThrowExceptionWhen_PageLinksCrawlDisallowed);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(0, _pageCrawlStartingCount);
            Assert.AreEqual(0, _pageCrawlCompletedCount);
            Assert.AreEqual(1, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }

        [Test]
        public void Crawl_PageLinksCrawlDisallowedSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += new EventHandler<PageLinksCrawlDisallowedArgs>(ThrowExceptionWhen_PageLinksCrawlDisallowed);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(1, _pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }


        [Test]
        public void Crawl_PageCrawlStartingEvent_IsSynchronous()
        {
            int elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaaa" });

            _unitUnderTest.PageCrawlStarting += new EventHandler<PageCrawlStartingArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 800);
        }

        [Test]
        public void Crawl_PageCrawlCompletedEvent_IsSynchronous()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            int elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri))).Returns(new List<Uri>(){
                new Uri(_rootUri.AbsoluteUri + "page2.html"), //should be fired sync
                new Uri(_rootUri.AbsoluteUri + "page3.html"), //should be fired sync
                new Uri(_rootUri.AbsoluteUri + "page4.html"),  //should be fired sync
                new Uri(_rootUri.AbsoluteUri + "page5.html")}); //should be fired sync since its the last page to be crawled
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            _unitUnderTest.PageCrawlCompleted += new EventHandler<PageCrawlCompletedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 4 * elapsedTimeForLongJob);
        }

        [Test]
        public void Crawl_PageCrawlDisallowedEvent_IsSynchronous()
        {
            int elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            _unitUnderTest.PageCrawlDisallowed += new EventHandler<PageCrawlDisallowedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 800);
        }

        [Test]
        public void Crawl_PageLinksCrawlDisallowedEvent_IsSynchronous()
        {
            int elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            _unitUnderTest.PageLinksCrawlDisallowed += new EventHandler<PageLinksCrawlDisallowedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 800);
        }

        #endregion

        #region Async Event Tests

        [Test]
        public void Crawl_ShouldFire_RobotsTxtParseCompletedAsync()
        {
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(new RobotsDotText(_rootUri, string.Empty));

            int _pageRobotsTxtCompleted = 0;

            _unitUnderTest.RobotsDotTextParseCompletedAsync += (s, e) => ++_pageRobotsTxtCompleted;

            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(100);
            Assert.AreEqual(1, _pageRobotsTxtCompleted);
        }

        [Test]
        public void Crawl_CrawlDecisionMakerMethodsReturnTrue_PageCrawlStartingAndCompletedAsyncEventsFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompletedAsync += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStartingAsync += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowedAsync += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowedAsync += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(100);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
        }

        [Test]
        public void Crawl_CrawlDecisionMakerShouldCrawlLinksMethodReturnsFalse_PageLinksCrawlDisallowedAsyncEventFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompletedAsync += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStartingAsync += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowedAsync += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowedAsync += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(100);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(1, _pageLinksCrawlDisallowedCount);
        }

        [Test]
        public void Crawl_CrawlDecisionMakerShouldCrawlMethodReturnsFalse_PageCrawlDisallowedAsyncEventFires()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompletedAsync += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStartingAsync += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowedAsync += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowedAsync += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(100);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Never());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Never());

            Assert.AreEqual(0, _pageCrawlStartingCount);
            Assert.AreEqual(0, _pageCrawlCompletedCount);
            Assert.AreEqual(1, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
        }


        [Test]
        public void Crawl_PageCrawlStartingAndCompletedAsyncEventSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompletedAsync += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStartingAsync += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlStartingAsync += new EventHandler<PageCrawlStartingArgs>(ThrowExceptionWhen_PageCrawlStarting);
            _unitUnderTest.PageCrawlCompletedAsync += new EventHandler<PageCrawlCompletedArgs>(ThrowExceptionWhen_PageCrawlCompleted);
            _unitUnderTest.PageCrawlDisallowedAsync += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowedAsync += (s, e) => ++_pageLinksCrawlDisallowedCount;

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(1000);//sleep since the events are async and may not complete

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }

        [Test]
        public void Crawl_PageCrawlDisallowedAsyncSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompletedAsync += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStartingAsync += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowedAsync += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowedAsync += (s, e) => ++_pageLinksCrawlDisallowedCount;
            _unitUnderTest.PageCrawlDisallowedAsync += new EventHandler<PageCrawlDisallowedArgs>(ThrowExceptionWhen_PageCrawlDisallowed);
            _unitUnderTest.PageLinksCrawlDisallowedAsync += new EventHandler<PageLinksCrawlDisallowedArgs>(ThrowExceptionWhen_PageLinksCrawlDisallowed);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(1500);//sleep since the events are async and may not complete

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(0, _pageCrawlStartingCount);
            Assert.AreEqual(0, _pageCrawlCompletedCount);
            Assert.AreEqual(1, _pageCrawlDisallowedCount);
            Assert.AreEqual(0, _pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }

        [Test]
        public void Crawl_PageLinksCrawlDisallowedAsyncSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompletedAsync += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStartingAsync += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowedAsync += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowedAsync += (s, e) => ++_pageLinksCrawlDisallowedCount;
            //_unitUnderTest.PageLinksCrawlDisallowed += new EventHandler<PageLinksCrawlDisallowedArgs>(ThrowExceptionWhen_PageLinksCrawlDisallowed);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            System.Threading.Thread.Sleep(2000);//sleep since the events are async and may not complete, set to 2 seconds since this test was mysteriously failing only when run with code coverage

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(1, _pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }


        [Test]
        public void Crawl_PageCrawlStartingAsyncEvent_IsAsynchronous()
        {
            int elapsedTimeForLongJob = 5000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaaa" });

            _unitUnderTest.PageCrawlStartingAsync += new EventHandler<PageCrawlStartingArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < elapsedTimeForLongJob);
        }

        [Test]
        public void Crawl_PageCrawlCompletedAsyncEvent_IsAsynchronous()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            int elapsedTimeForLongJob = 2000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri))).Returns(new List<Uri>(){
                new Uri(_rootUri.AbsoluteUri + "page2.html"), //should be fired async
                new Uri(_rootUri.AbsoluteUri + "page3.html"), //should be fired async
                new Uri(_rootUri.AbsoluteUri + "page4.html"),  //should be fired async
                new Uri(_rootUri.AbsoluteUri + "page5.html")}); //should be fired SYNC since its the last page to be crawled
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            
            _unitUnderTest.PageCrawlCompletedAsync += new EventHandler<PageCrawlCompletedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            //The root uri and last page should be fired synchronously but all other async
            Assert.IsTrue(timer.ElapsedMilliseconds > elapsedTimeForLongJob); //Last page is run synchronously
            Assert.IsTrue(timer.ElapsedMilliseconds < 4 * elapsedTimeForLongJob); //All but 1 should have been processed async
        }

        [Test]
        public void Crawl_PageCrawlDisallowedAsyncEvent_IsAsynchronous()
        {
            int elapsedTimeForLongJob = 5000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            _unitUnderTest.PageCrawlDisallowedAsync += new EventHandler<PageCrawlDisallowedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < elapsedTimeForLongJob);
        }

        [Test]
        public void Crawl_PageLinksCrawlDisallowedAsyncEvent_IsAsynchronous()
        {
            int elapsedTimeForLongJob = 5000;

            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            _unitUnderTest.PageLinksCrawlDisallowedAsync += new EventHandler<PageLinksCrawlDisallowedArgs>((sender, args) => System.Threading.Thread.Sleep(elapsedTimeForLongJob));

            Stopwatch timer = Stopwatch.StartNew();
            _unitUnderTest.Crawl(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < elapsedTimeForLongJob);
        }

        #endregion

        [Test]
        public void Crawl_CrawlDecisionDelegatesReturnTrue_EventsFired()
        {
            //Arrange
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri> { new Uri("http://a.com/a"), new Uri("http://a.com/b") });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            int pageCrawlStartingCount = 0;
            int pageCrawlCompletedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;

            bool shouldCrawlPageDelegateCalled = false;
            _unitUnderTest.ShouldCrawlPage((x, y) =>
            {
                if (shouldCrawlPageDelegateCalled)
                {
                    return new CrawlDecision { Allow = false };
                }
                else
                {
                    //only return true on the first call to avoid an infinite loop
                    shouldCrawlPageDelegateCalled = true;
                    return new CrawlDecision { Allow = true };
                }
            });

            bool shouldCrawlPageLinksDelegateCalled = false;
            _unitUnderTest.ShouldCrawlPageLinks((x, y) =>
            {
                shouldCrawlPageLinksDelegateCalled = true;
                return new CrawlDecision { Allow = true };
            });

            int isInternalUriDelegateCalledCount = 0;
            _unitUnderTest.IsInternalUri((x, y) =>
            {
                isInternalUriDelegateCalledCount++;
                return true;
            });

            //Act
            _unitUnderTest.Crawl(_rootUri);

            //Assert
            System.Threading.Thread.Sleep(150);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(3));//1 for _rootUri, 2 for the returned links
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.IsTrue(shouldCrawlPageDelegateCalled);
            //Assert.IsTrue(shouldCrawlDownloadPageContentDelegateCalled);
            Assert.IsTrue(shouldCrawlPageLinksDelegateCalled);
            Assert.AreEqual(2, isInternalUriDelegateCalledCount);
            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
        }

        [Test]
        public void Crawl_ShouldCrawlPageDelegateReturnsFalse_PageIsNotCrawled()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            int pageCrawlStartingCount = 0;
            int pageCrawlCompletedCount = 0;
            int pageCrawlDisallowedCount = 0;
            int pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            bool shouldCrawlPageDelegateCalled = false;
            _unitUnderTest.ShouldCrawlPage((x, y) =>
            {
                shouldCrawlPageDelegateCalled = true;
                return new CrawlDecision { Allow = false, Reason = "aaa" };
            });

            _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.IsTrue(shouldCrawlPageDelegateCalled);
            Assert.AreEqual(0, pageCrawlStartingCount);
            Assert.AreEqual(0, pageCrawlCompletedCount);
            Assert.AreEqual(1, pageCrawlDisallowedCount);
            Assert.AreEqual(0, pageLinksCrawlDisallowedCount);
        }

        [Test]
        public void Crawl_ShouldFire_RobotsTxtParseCompleted()
        {
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeRobotsDotTextFinder.Setup(f => f.Find(It.IsAny<Uri>())).Returns(new RobotsDotText(_rootUri, string.Empty));

            int _pageRobotsTxtCompleted = 0;

            _unitUnderTest.RobotsDotTextParseCompleted += (s, e) => ++_pageRobotsTxtCompleted;

            _unitUnderTest.Crawl(_rootUri);

            Assert.AreEqual(1, _pageRobotsTxtCompleted);
        }

        [Test]
        public void Crawl_ShouldCrawlLinksDelegateReturnsFalse_PageLinksNotCrawled()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            int _pageCrawlStartingCount = 0;
            int _pageCrawlCompletedCount = 0;
            int _pageCrawlDisallowedCount = 0;
            int _pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++_pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++_pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++_pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++_pageLinksCrawlDisallowedCount;

            _unitUnderTest.ShouldCrawlPageLinks((x, y) =>
            {
                return new CrawlDecision { Allow = false, Reason = "aaa" };
            });

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, _pageCrawlStartingCount);
            Assert.AreEqual(1, _pageCrawlCompletedCount);
            Assert.AreEqual(0, _pageCrawlDisallowedCount);
            Assert.AreEqual(1, _pageLinksCrawlDisallowedCount);
        }

        [Test]
        public void Crawl_CrawlResult_CrawlContextIsSet()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            Assert.IsNotNull(result.CrawlContext);
            Assert.AreSame(_dummyScheduler, result.CrawlContext.Scheduler);
        }

        [Test]
        public void Crawl_StopRequested_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            PageToCrawl pageToReturn = new PageToCrawl(_rootUri);
            for (int i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _unitUnderTest.PageCrawlStarting += (e, a) =>
            {
                a.CrawlContext.IsCrawlStopRequested = true;
                System.Threading.Thread.Sleep(500);
            };

            //Act
            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            //Assert
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsTrue(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [Test]
        public void Crawl_HardStopRequested_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            PageToCrawl pageToReturn = new PageToCrawl(_rootUri);
            for (int i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _unitUnderTest.PageCrawlStarting += (e, a) =>
            {
                a.CrawlContext.IsCrawlHardStopRequested = true;
                System.Threading.Thread.Sleep(500);
            };

            //Act
            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            //Assert
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.AtMost(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [Test]
        public void Crawl_CancellationRequested_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            System.Timers.Timer timer = new System.Timers.Timer(10);
            timer.Elapsed += (o, e) =>
            {
                cancellationTokenSource.Cancel();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();

            PageToCrawl pageToReturn = new PageToCrawl(_rootUri);
            for (int i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            //Act
            CrawlResult result = _unitUnderTest.Crawl(_rootUri, cancellationTokenSource);

            System.Threading.Thread.Sleep(30);

            //Assert
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
            Assert.IsTrue(result.CrawlContext.CancellationTokenSource.IsCancellationRequested);
        }

        [Test]
        public void Crawl_CancellationRequestedThroughCrawlDecisionCall_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            PageToCrawl pageToReturn = new PageToCrawl(_rootUri);
            for (int i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()))
            .Callback<PageToCrawl, CrawlContext>((p, c) =>
            {
                c.CancellationTokenSource.Cancel();
                System.Threading.Thread.Sleep(500);
            })
            .Returns(new CrawlDecision { Allow = false, Reason = "Should have timed out so this crawl decision doesn't matter." });

            //Act
            CrawlResult result = _unitUnderTest.Crawl(_rootUri, cancellationTokenSource);

            //Assert
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
            Assert.IsTrue(result.CrawlContext.CancellationTokenSource.IsCancellationRequested);
        }


        [Test]
        public void Crawl_OverCrawlTimeoutSeconds_CrawlIsStoppedBeforeCompletion()
        {
            _dummyConfiguration.CrawlTimeoutSeconds = 1;

            PageToCrawl pageToReturn = new PageToCrawl(_rootUri);
            CrawledPage crawledPage = new CrawledPage(_rootUri) { ParentUri = _rootUri };

            for (int i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()))
                .Callback(() => System.Threading.Thread.Sleep(2000))
                .Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(crawledPage);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [Test]
        public void CrawlBag_IsSetOnCrawlContext()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(new CrawledPage(_rootUri));
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            CrawlContext actualCrawlContext = null;

            _unitUnderTest.PageCrawlCompleted += (s, e) => actualCrawlContext = e.CrawlContext;

            _unitUnderTest.Crawl(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHyperLinkParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual("SomeVal", actualCrawlContext.CrawlBag.SomeVal);
            Assert.AreEqual(2, actualCrawlContext.CrawlBag.SomeList.Count);
        }

        [Test]
        public void Crawl_NotEnoughAvailableMemoryToStartTheCrawl_CrawlIsStoppedBeforeStarting()
        {
            _dummyConfiguration.MinAvailableMemoryRequiredInMb = int.MaxValue;
            _fakeMemoryManager.Setup(f => f.IsSpaceAvailable(It.IsAny<int>())).Returns(false);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            Assert.AreEqual(1, _dummyScheduler.Count);//no need to clear the scheduler since the crawl was never started
            Assert.IsTrue(result.ErrorOccurred);
            Assert.IsTrue(result.ErrorException is InsufficientMemoryException);
            Assert.AreEqual("Process does not have the configured [2147483647mb] of available memory to crawl site [http://a.com/]. This is configurable through the minAvailableMemoryRequiredInMb in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.", result.ErrorException.Message);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [Test]
        public void Crawl_CrawlHasExceededMaxMemoryUsageInMb_CrawlIsStoppedBeforeCompletion()
        {
            _dummyConfiguration.MaxMemoryUsageInMb = 1;
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(2);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);

            _fakeMemoryManager.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(2));

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsTrue(result.ErrorOccurred);
            Assert.IsTrue(result.ErrorException is InsufficientMemoryException);
            Assert.AreEqual("Process is using [2mb] of memory which is above the max configured of [1mb] for site [http://a.com/]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", result.ErrorException.Message);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [Test]
        public void Crawl_ExtractedLinksAreNotCheckedTwice()
        {
            Uri fakeLink1 = new Uri("http://a.com/someUri");
            Uri fakeLink2 = new Uri("http://a.com/someOtherUri");
            Uri fakeLink3 = new Uri("http://a.com/anotherOne");
            CrawledPage homePage = new CrawledPage(_rootUri);
            CrawledPage page1 = new CrawledPage(fakeLink1);
            CrawledPage page2 = new CrawledPage(fakeLink2);

            // All links are found in each pages.
            _fakeHyperLinkParser.Setup(parser => parser.GetLinks(It.IsAny<CrawledPage>())).Returns(new [] { fakeLink1, fakeLink2, fakeLink3 });
            
            _fakeHttpRequester.Setup(f => f.MakeRequest(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(homePage);
            _fakeHttpRequester.Setup(f => f.MakeRequest(fakeLink1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page1);
            _fakeHttpRequester.Setup(f => f.MakeRequest(fakeLink2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page2);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{Allow = true});
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == fakeLink3), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{Allow = false});
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _unitUnderTest.Crawl(_rootUri);

            // The links should be checked only one time, so ShouldCrawlPage should be called only 4 times.
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(4));
            _fakeHyperLinkParser.VerifyAll();
            _fakeCrawlDecisionMaker.VerifyAll();
        }

        [Test]
        public void Crawl_CanExtractRetryAfterTimeFromHeaders()
        {
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeCrawlDecisionMaker.SetupSequence(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()))
                .Returns(new CrawlDecision { Allow = true })
                .Returns(new CrawlDecision { Allow = false });
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());

            CrawledPage page = new CrawledPage(_rootUri) {
                WebException = new WebException(),
                HttpWebResponse = new HttpWebResponseWrapper(HttpStatusCode.ServiceUnavailable,
                    "",
                    null,
                    new WebHeaderCollection{ {"Retry-After", "1"} })
            };
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            watch.Start();

            Assert.That(watch.ElapsedMilliseconds, Is.GreaterThan(2000));
            Assert.That(page.RetryAfter, Is.EqualTo(1.0));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(2));
        }
        
        [Test]
        public void Crawl_CanExtractRetryAfterDateFromHeaders()
        {
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHyperLinkParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeCrawlDecisionMaker.SetupSequence(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()))
                .Returns(new CrawlDecision { Allow = true })
                .Returns(new CrawlDecision { Allow = false });
            _fakeHyperLinkParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<Uri>());

            CrawledPage page = new CrawledPage(_rootUri) {
                WebException = new WebException(),
                HttpWebResponse = new HttpWebResponseWrapper(HttpStatusCode.ServiceUnavailable,
                    "",
                    null,
                    new WebHeaderCollection{ {"Retry-After", DateTime.Now.AddSeconds(1).ToString() } })
            };
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page);

            Stopwatch watch = new Stopwatch();
            watch.Start();
            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            watch.Start();

            Assert.That(watch.ElapsedMilliseconds, Is.GreaterThan(2000));
            Assert.That(page.RetryAfter, Is.GreaterThan(0));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(2));
        }

        [Test]
        public void Crawl_ChangeRootUriIfRedirected()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _dummyConfiguration.IsHttpRequestAutoRedirectsEnabled = false;

            // Setup a root page that was redirected.
            Uri redirectedUri = new Uri("http://www.domain.com/");
            CrawledPage page = new CrawledPage(_rootUri) {
                WebException = new WebException(),
                HttpWebResponse = new HttpWebResponseWrapper {
                    StatusCode = HttpStatusCode.Redirect,
                    Headers = new WebHeaderCollection { { "Location", redirectedUri.AbsoluteUri } }
                }
            };
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            Assert.That(result.CrawlContext.RootUri.AbsoluteUri, Is.EqualTo(redirectedUri));
            Assert.That(result.CrawlContext.OriginalRootUri, Is.EqualTo(_rootUri));
        }

        [Test]
        public void Crawl_ChangeRootUriIfRedirectedAutomatically()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _dummyConfiguration.IsHttpRequestAutoRedirectsEnabled = true;

            // Setup a root page that was redirected.
            Uri redirectedUri = new Uri("http://www.domain.com/");
            CrawledPage page = new CrawledPage(_rootUri) {
                WebException = new WebException(),
                HttpWebResponse = new HttpWebResponseWrapper {
                    StatusCode = HttpStatusCode.OK,
                    ResponseUri = redirectedUri
                }
            };
            _fakeHttpRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(page);

            CrawlResult result = _unitUnderTest.Crawl(_rootUri);
            Assert.That(result.CrawlContext.RootUri.AbsoluteUri, Is.EqualTo(redirectedUri));
            Assert.That(result.CrawlContext.OriginalRootUri, Is.EqualTo(_rootUri));
        }

        private void ThrowExceptionWhen_PageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            throw new Exception("no!!!");
        }

        private void ThrowExceptionWhen_PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            throw new Exception("Oh No!");
        }

        private void ThrowExceptionWhen_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            throw new Exception("no!!!");
        }

        private void ThrowExceptionWhen_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            throw new Exception("Oh No!");
        }

        private HtmlDocument GetHtmlDocument(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }
    }
}
