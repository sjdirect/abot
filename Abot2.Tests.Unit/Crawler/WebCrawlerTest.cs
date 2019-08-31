using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CrawlDecision = Abot2.Poco.CrawlDecision;
using HyperLink = Abot2.Poco.HyperLink;

namespace Abot2.Tests.Unit.Crawler
{
    [TestClass]
    public class WebCrawlerTest
    {
        IPoliteWebCrawler _unitUnderTest;
        Mock<IPageRequester> _fakeHttpRequester;
        Mock<IHtmlParser> _fakeHtmlParser;
        Mock<ICrawlDecisionMaker> _fakeCrawlDecisionMaker;
        Mock<IMemoryManager> _fakeMemoryManager;
        Mock<IDomainRateLimiter> _fakeDomainRateLimiter;
        Mock<IRobotsDotTextFinder> _fakeRobotsDotTextFinder;
        
        Scheduler _dummyScheduler;
        TaskThreadManager _dummyThreadManager;
        CrawlConfiguration _dummyConfiguration;
        Uri _rootUri;

        [TestInitialize]
        public void SetUp()
        {
            _fakeHtmlParser = new Mock<IHtmlParser>();
            _fakeHttpRequester = new Mock<IPageRequester>();
            _fakeCrawlDecisionMaker = new Mock<ICrawlDecisionMaker>();
            _fakeMemoryManager = new Mock<IMemoryManager>();
            _fakeDomainRateLimiter = new Mock<IDomainRateLimiter>();
            _fakeRobotsDotTextFinder = new Mock<IRobotsDotTextFinder>();


            _dummyScheduler = new Scheduler();
            _dummyThreadManager = new TaskThreadManager(10);
            _dummyConfiguration = new CrawlConfiguration();
            _dummyConfiguration.ConfigurationExtensions.Add("somekey", "someval");

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _unitUnderTest.CrawlBag.SomeVal = "SomeVal";
            _unitUnderTest.CrawlBag.SomeList = new List<string>() { "a", "b" };
            _rootUri = new Uri("http://a.com/");
        }

        [TestMethod]
        public void Constructor_BuildsInstanceWithoutError()
        {
            var unused = new PoliteWebCrawler();
            unused.Dispose();
        }

        [TestMethod]
        public void Constructor_ConfigValueMaxConcurrentThreadsIsZero_DoesNotThrowException()
        {
            _dummyConfiguration.MaxConcurrentThreads = 0;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, null, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
        }

        [TestMethod]
        public async Task Crawl_CallsDependencies()
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
                new HyperLink(){ HrefValue = uri1 },
                new HyperLink(){ HrefValue = uri2 }
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{Allow = true});
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{ Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri)), Times.Exactly(1));
            _fakeHtmlParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == uri1)), Times.Exactly(1));
            _fakeHtmlParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == uri2)), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Crawl_NullUri_ThrowsException()
        {
            await _unitUnderTest.CrawlAsync(null);
        }

        [TestMethod]
        public async Task Crawl_ExceptionThrownByScheduler_SetsCrawlResultError()
        {
            var fakeScheduler = new Mock<IScheduler>();
            var ex = new Exception("oh no");
            fakeScheduler.Setup(f => f.Count).Throws(ex);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, fakeScheduler.Object, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            fakeScheduler.Verify(f => f.Count, Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
        }

        [TestMethod]
        public async Task Crawl_ExceptionThrownByFirstShouldSchedulePageLink_SetsCrawlResultError()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            var ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Throws(ex);

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_SingleThread_ExceptionThrownDuringProcessPage_SetsCrawlResultError()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            var ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Throws(ex);

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }


        [TestMethod]
        public async Task Crawl_MultiThread_ExceptionThrownDuringProcessPage_SetsCrawlResultError()
        {
            var ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Throws(ex);

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        #region Synchronous Event Tests

        [TestMethod]
        public async Task Crawl_CrawlDecisionMakerMethodsReturnTrue_PageCrawlStartingAndCompletedEventsFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
            Assert.AreEqual(0, pageCrawlDisallowedCount);
            Assert.AreEqual(0, pageLinksCrawlDisallowedCount);
        }

        [TestMethod]
        public async Task Crawl_CrawlDecisionMakerShouldCrawlLinksMethodReturnsFalse_PageLinksCrawlDisallowedEventFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
            Assert.AreEqual(0, pageCrawlDisallowedCount);
            Assert.AreEqual(1, pageLinksCrawlDisallowedCount);
        }

        [TestMethod]
        public async Task Crawl_CrawlDecisionMakerShouldCrawlMethodReturnsFalse_PageCrawlDisallowedEventFires()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>()), Times.Never());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Never());

            Assert.AreEqual(0, pageCrawlStartingCount);
            Assert.AreEqual(0, pageCrawlCompletedCount);
            Assert.AreEqual(1, pageCrawlDisallowedCount);
            Assert.AreEqual(0, pageLinksCrawlDisallowedCount);
        }


        [TestMethod]
        public async Task Crawl_PageCrawlStartingAndCompletedEventSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlStarting += ThrowExceptionWhen_PageCrawlStarting;
            _unitUnderTest.PageCrawlCompleted += ThrowExceptionWhen_PageCrawlCompleted;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
            Assert.AreEqual(0, pageCrawlDisallowedCount);
            Assert.AreEqual(0, pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }

        [TestMethod]
        public async Task Crawl_PageCrawlDisallowedSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;
            _unitUnderTest.PageCrawlDisallowed += ThrowExceptionWhen_PageCrawlDisallowed;
            _unitUnderTest.PageLinksCrawlDisallowed += ThrowExceptionWhen_PageLinksCrawlDisallowed;

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(0, pageCrawlStartingCount);
            Assert.AreEqual(0, pageCrawlCompletedCount);
            Assert.AreEqual(1, pageCrawlDisallowedCount);
            Assert.AreEqual(0, pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }

        [TestMethod]
        public async Task Crawl_PageLinksCrawlDisallowedSubscriberThrowsExceptions_DoesNotCrash()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += ThrowExceptionWhen_PageLinksCrawlDisallowed;

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
            Assert.AreEqual(0, pageCrawlDisallowedCount);
            Assert.AreEqual(1, pageLinksCrawlDisallowedCount);
            Assert.IsFalse(result.ErrorOccurred);
        }


        [TestMethod]
        public async Task Crawl_PageCrawlStartingEvent_IsSynchronous()
        {
            var elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaaa" });

            _unitUnderTest.PageCrawlStarting += (sender, args) => Thread.Sleep(elapsedTimeForLongJob);

            var timer = Stopwatch.StartNew();
            await _unitUnderTest.CrawlAsync(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 800);
        }

        [TestMethod]
        public async Task Crawl_PageCrawlCompletedEvent_IsSynchronous()
        {
            _dummyThreadManager = new TaskThreadManager(1);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            var elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri))).Returns(new List<HyperLink>(){
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page2.html")}, //should be fired sync
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page3.html")}, //should be fired sync
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page4.html")},  //should be fired sync
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page5.html")}}); //should be fired sync since its the last page to be crawled
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            _unitUnderTest.PageCrawlCompleted += (sender, args) => Thread.Sleep(elapsedTimeForLongJob);

            var timer = Stopwatch.StartNew();
            await _unitUnderTest.CrawlAsync(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 4 * elapsedTimeForLongJob);
        }

        [TestMethod]
        public async Task Crawl_PageCrawlDisallowedEvent_IsSynchronous()
        {
            var elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            _unitUnderTest.PageCrawlDisallowed += (sender, args) => Thread.Sleep(elapsedTimeForLongJob);

            var timer = Stopwatch.StartNew();
            await _unitUnderTest.CrawlAsync(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 800);
        }

        [TestMethod]
        public async Task Crawl_PageLinksCrawlDisallowedEvent_IsSynchronous()
        {
            var elapsedTimeForLongJob = 1000;

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            _unitUnderTest.PageLinksCrawlDisallowed += (sender, args) => Thread.Sleep(elapsedTimeForLongJob);

            var timer = Stopwatch.StartNew();
            await _unitUnderTest.CrawlAsync(_rootUri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 800);
        }

        #endregion


        [TestMethod]
        public async Task Crawl_CrawlDecisionDelegatesReturnTrue_EventsFired()
        {
            //Arrange
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>
            {
                new HyperLink{ HrefValue = new Uri("http://a.com/a") },
                new HyperLink{ HrefValue = new Uri("http://a.com/b") }
            });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;

            var shouldCrawlPageDelegateCalled = false;
            _unitUnderTest.ShouldCrawlPageDecisionMaker = (x, y) =>
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
            };

            var shouldCrawlPageLinksDelegateCalled = false;
            _unitUnderTest.ShouldCrawlPageLinksDecisionMaker = (x, y) =>
            {
                shouldCrawlPageLinksDelegateCalled = true;
                return new CrawlDecision { Allow = true };
            };

            var isInternalUriDelegateCalledCount = 0;
            _unitUnderTest.IsInternalUriDecisionMaker = (x, y) =>
            {
                isInternalUriDelegateCalledCount++;
                return true;
            };

            //Act
            await _unitUnderTest.CrawlAsync(_rootUri);

            //Assert
            Thread.Sleep(150);//sleep since the events are async and may not complete before returning

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(3));//1 for _rootUri, 2 for the returned links
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.IsTrue(shouldCrawlPageDelegateCalled);
            //Assert.IsTrue(shouldCrawlDownloadPageContentDelegateCalled);
            Assert.IsTrue(shouldCrawlPageLinksDelegateCalled);
            Assert.AreEqual(2, isInternalUriDelegateCalledCount);
            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
        }

        [TestMethod]
        public async Task Crawl_ShouldCrawlPageDelegateReturnsFalse_PageIsNotCrawled()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            var shouldCrawlPageDelegateCalled = false;
            _unitUnderTest.ShouldCrawlPageDecisionMaker = (x, y) =>
            {
                shouldCrawlPageDelegateCalled = true;
                return new CrawlDecision { Allow = false, Reason = "aaa" };
            };

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.IsTrue(shouldCrawlPageDelegateCalled);
            Assert.AreEqual(0, pageCrawlStartingCount);
            Assert.AreEqual(0, pageCrawlCompletedCount);
            Assert.AreEqual(1, pageCrawlDisallowedCount);
            Assert.AreEqual(0, pageLinksCrawlDisallowedCount);
        }

        [TestMethod]
        public async Task Crawl_ShouldFire_RobotsTxtParseCompleted()
        {
            _dummyConfiguration.IsRespectRobotsDotTextEnabled = true;
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>())).Returns(Task.FromResult(new RobotsDotText(_rootUri, string.Empty) as IRobotsDotText));

            var pageRobotsTxtCompleted = 0;

            _unitUnderTest.RobotsDotTextParseCompleted += (s, e) => ++pageRobotsTxtCompleted;

            await _unitUnderTest.CrawlAsync(_rootUri);

            Assert.AreEqual(1, pageRobotsTxtCompleted);
        }

        [TestMethod]
        public async Task Crawl_ShouldCrawlLinksDelegateReturnsFalse_PageLinksNotCrawled()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var pageCrawlStartingCount = 0;
            var pageCrawlCompletedCount = 0;
            var pageCrawlDisallowedCount = 0;
            var pageLinksCrawlDisallowedCount = 0;
            _unitUnderTest.PageCrawlCompleted += (s, e) => ++pageCrawlCompletedCount;
            _unitUnderTest.PageCrawlStarting += (s, e) => ++pageCrawlStartingCount;
            _unitUnderTest.PageCrawlDisallowed += (s, e) => ++pageCrawlDisallowedCount;
            _unitUnderTest.PageLinksCrawlDisallowed += (s, e) => ++pageLinksCrawlDisallowedCount;

            _unitUnderTest.ShouldCrawlPageLinksDecisionMaker = ((x, y) => new CrawlDecision { Allow = false, Reason = "aaa" });

            await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Never());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual(1, pageCrawlStartingCount);
            Assert.AreEqual(1, pageCrawlCompletedCount);
            Assert.AreEqual(0, pageCrawlDisallowedCount);
            Assert.AreEqual(1, pageLinksCrawlDisallowedCount);
        }

        [TestMethod]
        public async Task Crawl_CrawlResult_CrawlContextIsSet()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());
            Assert.IsNotNull(result.CrawlContext);
            Assert.AreSame(_dummyScheduler, result.CrawlContext.Scheduler);
        }

        [TestMethod]
        public async Task Crawl_StopRequested_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            var pageToReturn = new PageToCrawl(_rootUri);
            for (var i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _unitUnderTest.PageCrawlStarting += (e, a) =>
            {
                a.CrawlContext.IsCrawlStopRequested = true;
                Thread.Sleep(500);
            };

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            //Assert
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsTrue(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_HardStopRequested_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            var pageToReturn = new PageToCrawl(_rootUri);
            for (var i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _unitUnderTest.PageCrawlStarting += (e, a) =>
            {
                a.CrawlContext.IsCrawlHardStopRequested = true;
                Thread.Sleep(500);
            };

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            //Assert
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.AtMost(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_CancellationRequested_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var timer = new System.Timers.Timer(10);
            timer.Elapsed += (o, e) =>
            {
                cancellationTokenSource.Cancel();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();

            var pageToReturn = new PageToCrawl(_rootUri);
            for (var i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri, cancellationTokenSource);

            Thread.Sleep(30);

            //Assert
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
            Assert.IsTrue(result.CrawlContext.CancellationTokenSource.IsCancellationRequested);

            timer.Dispose();
        }

        [TestMethod]
        public async Task Crawl_CancellationRequestedThroughCrawlDecisionCall_CrawlIsStoppedBeforeCompletion()
        {
            //Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var pageToReturn = new PageToCrawl(_rootUri);
            for (var i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()))
            .Callback<PageToCrawl, CrawlContext>((p, c) =>
            {
                c.CancellationTokenSource.Cancel();
                Thread.Sleep(500);
            })
            .Returns(new CrawlDecision { Allow = false, Reason = "Should have timed out so this crawl decision doesn't matter." });

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri, cancellationTokenSource);

            //Assert
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
            Assert.IsTrue(result.CrawlContext.CancellationTokenSource.IsCancellationRequested);
        }


        [TestMethod]
        public async Task Crawl_OverCrawlTimeoutSeconds_CrawlIsStoppedBeforeCompletion()
        {
            _dummyConfiguration.CrawlTimeoutSeconds = 1;

            var pageToReturn = new PageToCrawl(_rootUri);
            var crawledPage = new CrawledPage(_rootUri) { ParentUri = _rootUri };

            for (var i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()))
                .Callback(() => Thread.Sleep(2000))
                .Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(crawledPage));

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public void CrawlBag_IsSetOnCrawlContext()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            CrawlContext actualCrawlContext = null;

            _unitUnderTest.PageCrawlCompleted += (s, e) => actualCrawlContext = e.CrawlContext;

            _unitUnderTest.CrawlAsync(_rootUri);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.IsAny<CrawledPage>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Once());
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Once());

            Assert.AreEqual("SomeVal", actualCrawlContext.CrawlBag.SomeVal);
            Assert.AreEqual(2, actualCrawlContext.CrawlBag.SomeList.Count);
        }

        [TestMethod]
        public async Task Crawl_NotEnoughAvailableMemoryToStartTheCrawl_CrawlIsStoppedBeforeStarting()
        {
            _dummyConfiguration.MinAvailableMemoryRequiredInMb = int.MaxValue;
            _fakeMemoryManager.Setup(f => f.IsSpaceAvailable(It.IsAny<int>())).Returns(false);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            Assert.AreEqual(1, _dummyScheduler.Count);//no need to clear the scheduler since the crawl was never started
            Assert.IsTrue(result.ErrorOccurred);
            Assert.IsTrue(result.ErrorException is InsufficientMemoryException);
            Assert.AreEqual("Process does not have the configured [2147483647mb] of available memory to crawl site [http://a.com/]. This is configurable through the minAvailableMemoryRequiredInMb in app.conf or CrawlConfiguration.MinAvailableMemoryRequiredInMb.", result.ErrorException.Message);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_CrawlHasExceededMaxMemoryUsageInMb_CrawlIsStoppedBeforeCompletion()
        {
            _dummyConfiguration.MaxMemoryUsageInMb = 1;
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(2);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            _fakeMemoryManager.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(2));

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsTrue(result.ErrorOccurred);
            Assert.IsTrue(result.ErrorException is InsufficientMemoryException);
            Assert.AreEqual("Process is using [2mb] of memory which is above the max configured of [1mb] for site [http://a.com/]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", result.ErrorException.Message);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod, Ignore("Having issues with this test")]
        public async Task Crawl_ExtractedLinksAreNotCheckedTwice()
        {
            var fakeLink1 = new Uri("http://a.com/someUri");
            var fakeLink2 = new Uri("http://a.com/someOtherUri");
            var fakeLink3 = new Uri("http://a.com/anotherOne");
            var homePage = new CrawledPage(_rootUri);
            var page1 = new CrawledPage(fakeLink1);
            var page2 = new CrawledPage(fakeLink2);
            var links = new List<HyperLink>
            {
                new HyperLink() {HrefValue = fakeLink1},
                new HyperLink() {HrefValue = fakeLink2},
                new HyperLink() {HrefValue = fakeLink3}
            };

            // All links are found in each pages.
            _fakeHtmlParser.Setup(parser => parser.GetLinks(It.IsAny<CrawledPage>())).Returns(links);

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(fakeLink1, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(fakeLink2, It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page2));
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == fakeLink3), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision {Allow = true});

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            await _unitUnderTest.CrawlAsync(_rootUri);

            // The links should be checked only one time, so ShouldCrawlPage should be called only 4 times.
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(4));
            _fakeHtmlParser.VerifyAll();
            _fakeCrawlDecisionMaker.VerifyAll();
        }






        [TestMethod]
        public async Task Crawl_CanExtractRetryAfterTimeFromHeaders()
        {
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeCrawlDecisionMaker.SetupSequence(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()))
                .Returns(new CrawlDecision { Allow = true })
                .Returns(new CrawlDecision { Allow = false });
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());

            var httpRequestMessage = new HttpRequestMessage() {RequestUri = new Uri("http://a.com")};
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = httpRequestMessage
            };
            httpResponseMessage.Headers.Add("Retry-After", "1");
            var page = new CrawledPage(_rootUri)
            {
                HttpRequestException = new HttpRequestException("aaa"),
                HttpRequestMessage = httpRequestMessage,
                HttpResponseMessage = httpResponseMessage
            };
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page));

            var watch = new Stopwatch();
            watch.Start();
            await _unitUnderTest.CrawlAsync(_rootUri);
            watch.Start();

            Assert.IsTrue(watch.ElapsedMilliseconds > 2000);
            Assert.AreEqual(page.RetryAfter, 1.0);
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Crawl_CanExtractRetryAfterDateFromHeaders()
        {
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyThreadManager, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeCrawlDecisionMaker.SetupSequence(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()))
                .Returns(new CrawlDecision { Allow = true })
                .Returns(new CrawlDecision { Allow = false });
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());

            var httpRequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://a.com") };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = httpRequestMessage
            };
            httpResponseMessage.Headers.Add("Retry-After", $"{DateTime.Now.AddSeconds(1):ddd,' 'dd' 'MMM' 'yyyy' 'HH':'mm':'ss' 'K}");
            var page = new CrawledPage(_rootUri)
            {
                HttpRequestException = new HttpRequestException("aaa"),
                HttpRequestMessage = httpRequestMessage,
                HttpResponseMessage = httpResponseMessage
            };

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page));

            var watch = new Stopwatch();
            watch.Start();
            await _unitUnderTest.CrawlAsync(_rootUri);
            watch.Start();

            Assert.IsTrue(watch.ElapsedMilliseconds > 2000);
            Assert.IsTrue(page.RetryAfter > 0);
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Crawl_ChangeRootUriIfRedirected()
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _dummyConfiguration.IsHttpRequestAutoRedirectsEnabled = false;

            // Setup a root page that was redirected.
            var redirectedUri = new Uri("http://www.domain.com/");
            var httpRequestMessage = new HttpRequestMessage() { RequestUri = _rootUri };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Redirect)
            {
                RequestMessage = httpRequestMessage
            };
            httpResponseMessage.Headers.Add("Location", redirectedUri.AbsoluteUri);
            var page = new CrawledPage(_rootUri)
            {
                HttpRequestMessage = httpRequestMessage,
                HttpResponseMessage = httpResponseMessage
            };
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>())).Returns(Task.FromResult(page));

            var result = await _unitUnderTest.CrawlAsync(_rootUri);

            Assert.AreEqual(result.CrawlContext.RootUri, redirectedUri);
            Assert.AreEqual(result.CrawlContext.OriginalRootUri, _rootUri);
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
    }
}
