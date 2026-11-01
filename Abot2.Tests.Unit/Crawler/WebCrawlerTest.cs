using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Abot2.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        CrawlConfiguration _dummyConfiguration;
        Uri _rootUri;

        public TestContext TestContext { get; set; }

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
            _dummyConfiguration = new CrawlConfiguration();
            _dummyConfiguration.ConfigurationExtensions.Add("somekey", "someval");

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
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
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
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

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(page2));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri))).Returns(links);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{Allow = true});
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri), It.IsAny<CrawlContext>())).Returns(new CrawlDecision{ Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(uri1, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(uri2, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
            _fakeHtmlParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == homePage.Uri)), Times.Exactly(1));
            _fakeHtmlParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == uri1)), Times.Exactly(1));
            _fakeHtmlParser.Verify(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == uri2)), Times.Exactly(1));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(3));
        }


        [TestMethod]
        public async Task Crawl_NullUri_ThrowsException()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _unitUnderTest.CrawlAsync(null, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task Crawl_ExceptionThrownByScheduler_SetsCrawlResultError()
        {
            var fakeScheduler = new Mock<IScheduler>();
            var ex = new Exception("oh no");
            fakeScheduler.Setup(f => f.Count).Throws(ex);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, fakeScheduler.Object, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            fakeScheduler.Verify(f => f.Count, Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
        }

        [TestMethod]
        public async Task Crawl_ExceptionThrownByFirstShouldSchedulePageLink_SetsCrawlResultError()
        {
            _dummyConfiguration.MaxConcurrentThreads = 1;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            var ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Throws(ex);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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
            _dummyConfiguration.MaxConcurrentThreads = 1;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            var ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Throws(ex);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Throws(ex);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_ExceptionThrownGettingNextPageFromScheduler_SetsCrawlResultError()
        {
            var ex = new Exception("oh no");
            var getNextCalled = false;
            var fakeScheduler = new Mock<IScheduler>();
            fakeScheduler.Setup(f => f.Count).Returns(() => Volatile.Read(ref getNextCalled) ? 0 : 1);
            fakeScheduler.Setup(f => f.GetNext()).Callback(() => Volatile.Write(ref getNextCalled, true)).Throws(ex);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, fakeScheduler.Object, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Never());
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_ExceptionThrownByAddPageToContextOverride_SetsCrawlResultError()
        {
            var ex = new Exception("oh no");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _unitUnderTest = new AddPageToContextThrowsCrawler(ex, _dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Never());
            Assert.IsTrue(result.ErrorOccurred);
            Assert.AreSame(ex, result.ErrorException);
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        private class AddPageToContextThrowsCrawler : PoliteWebCrawler
        {
            readonly Exception _exception;

            public AddPageToContextThrowsCrawler(Exception exception, CrawlConfiguration crawlConfiguration, ICrawlDecisionMaker crawlDecisionMaker, IScheduler scheduler, IPageRequester pageRequester, IHtmlParser htmlParser, IMemoryManager memoryManager, IDomainRateLimiter domainRateLimiter, IRobotsDotTextFinder robotsDotTextFinder)
                : base(crawlConfiguration, crawlDecisionMaker, scheduler, pageRequester, htmlParser, memoryManager, domainRateLimiter, robotsDotTextFinder)
            {
                _exception = exception;
            }

            protected override void AddPageToContext(PageToCrawl pageToCrawl)
            {
                throw _exception;
            }
        }

        #region Synchronous Event Tests

        [TestMethod]
        public async Task Crawl_CrawlDecisionMakerMethodsReturnTrue_PageCrawlStartingAndCompletedEventsFires()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
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

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
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

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Never());
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
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

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
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

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaaa" });

            using (var handler = new BlockingHandler(TestContext.CancellationToken))
            {
                _unitUnderTest.PageCrawlStarting += (sender, args) => handler.Handle();

                //Run the crawl on another thread, as some events fire inside CrawlAsync itself
                var crawl = Task.Run(() => _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken));

                //While the handler is held, a crawler that waits for its handlers cannot have finished
                handler.WaitUntilBlocked();
                Assert.IsFalse(crawl.IsCompleted, "The crawl should wait for the event handler");
                _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Never(), "The page should not be requested before the handler has finished");

                handler.Release();
                var result = await crawl;

                Assert.AreEqual(1, handler.FinishedCalls);
            }
        }

        [TestMethod]
        public async Task Crawl_PageCrawlCompletedEvent_IsSynchronous()
        {
            _dummyConfiguration.MaxConcurrentThreads = 1;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri))).Returns(new List<HyperLink>(){
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page2.html")}, //should be fired sync
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page3.html")}, //should be fired sync
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page4.html")},  //should be fired sync
                new HyperLink { HrefValue = new Uri(_rootUri.AbsoluteUri + "page5.html")}}); //should be fired sync since its the last page to be crawled
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            using (var handler = new BlockingHandler(TestContext.CancellationToken))
            {
                _unitUnderTest.PageCrawlCompleted += (sender, args) => handler.Handle();

                //Run the crawl on another thread, as some events fire inside CrawlAsync itself
                var crawl = Task.Run(() => _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken));

                //While the handler is held, a crawler that waits for its handlers cannot have finished
                handler.WaitUntilBlocked();
                Assert.IsFalse(crawl.IsCompleted, "The crawl should wait for the event handler");
                _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Never(), "The crawler should not move on before the handler has finished");

                handler.Release();
                var result = await crawl;

                Assert.AreEqual(result.CrawlContext.CrawledCount, handler.FinishedCalls);
            }
        }

        [TestMethod]
        public async Task Crawl_PageCrawlDisallowedEvent_IsSynchronous()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            using (var handler = new BlockingHandler(TestContext.CancellationToken))
            {
                _unitUnderTest.PageCrawlDisallowed += (sender, args) => handler.Handle();

                //Run the crawl on another thread, as some events fire inside CrawlAsync itself
                var crawl = Task.Run(() => _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken));

                //While the handler is held, a crawler that waits for its handlers cannot have finished
                handler.WaitUntilBlocked();
                Assert.IsFalse(crawl.IsCompleted, "The crawl should wait for the event handler");

                handler.Release();
                var result = await crawl;

                Assert.AreEqual(1, handler.FinishedCalls);
            }
        }

        [TestMethod]
        public async Task Crawl_PageLinksCrawlDisallowedEvent_IsSynchronous()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false, Reason = "aaa" });

            using (var handler = new BlockingHandler(TestContext.CancellationToken))
            {
                _unitUnderTest.PageLinksCrawlDisallowed += (sender, args) => handler.Handle();
                var completedEvents = 0;
                _unitUnderTest.PageCrawlCompleted += (sender, args) => Interlocked.Increment(ref completedEvents);

                //Run the crawl on another thread, as some events fire inside CrawlAsync itself
                var crawl = Task.Run(() => _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken));

                //While the handler is held, a crawler that waits for its handlers cannot have finished
                handler.WaitUntilBlocked();
                Assert.IsFalse(crawl.IsCompleted, "The crawl should wait for the event handler");
                Assert.AreEqual(0, Volatile.Read(ref completedEvents), "The page should not complete before the handler has finished");

                handler.Release();
                var result = await crawl;

                Assert.AreEqual(1, handler.FinishedCalls);
            }
        }

        #endregion


        [TestMethod]
        public async Task Crawl_CrawlDecisionDelegatesReturnTrue_EventsFired()
        {
            //Arrange
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
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
            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            //Assert
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeRobotsDotTextFinder.Setup(f => f.FindAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new RobotsDotText(_rootUri, string.Empty) as IRobotsDotText));

            var pageRobotsTxtCompleted = 0;

            _unitUnderTest.RobotsDotTextParseCompleted += (s, e) => ++pageRobotsTxtCompleted;

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.AreEqual(1, pageRobotsTxtCompleted);
        }

        [TestMethod]
        public async Task Crawl_ShouldCrawlLinksDelegateReturnsFalse_PageLinksNotCrawled()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
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

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _unitUnderTest.PageCrawlStarting += (e, a) =>
            {
                a.CrawlContext.IsCrawlStopRequested = true;
            };

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _unitUnderTest.PageCrawlStarting += (e, a) =>
            {
                a.CrawlContext.IsCrawlHardStopRequested = true;
            };

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var pageToReturn = new PageToCrawl(_rootUri);
                for (var i = 0; i < 100; i++)
                    _dummyScheduler.Add(pageToReturn);

                //The caller cancels while the first page is being processed, so the cancellation always arrives mid-crawl
                _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()))
                    .Callback(() => cancellationTokenSource.Cancel())
                    .Returns(new CrawlDecision { Allow = true });

                //Act
                var result = await _unitUnderTest.CrawlAsync(_rootUri, cancellationTokenSource.Token);

                //Assert
                _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
                Assert.AreEqual(0, _dummyScheduler.Count);
                Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
                Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
                Assert.IsTrue(result.CrawlContext.CancellationTokenSource.IsCancellationRequested);
                Assert.IsInstanceOfType<OperationCanceledException>(result.ErrorException);
            }
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
            })
            .Returns(new CrawlDecision { Allow = false, Reason = "Should have timed out so this crawl decision doesn't matter." });

            //Act
            var result = await _unitUnderTest.CrawlAsync(_rootUri, cancellationTokenSource.Token);

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
            var time = new ManualTimeProvider();
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object, time);
            bool? hardStoppedBeforeTimeout = null;

            var pageToReturn = new PageToCrawl(_rootUri);
            var crawledPage = new CrawledPage(_rootUri) { ParentUri = _rootUri };

            for (var i = 0; i < 100; i++)
                _dummyScheduler.Add(pageToReturn);

            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()))
                //The crawl clock reaches the timeout while the first page is being processed: 1 ms before it the crawl still runs, at it the crawl is hard stopped
                .Callback<PageToCrawl, CrawlContext>((p, c) =>
                {
                    time.Advance(TimeSpan.FromMilliseconds(999));
                    hardStoppedBeforeTimeout = c.IsCrawlHardStopRequested;
                    time.Advance(TimeSpan.FromMilliseconds(1));
                })
                .Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(crawledPage));

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.IsFalse(hardStoppedBeforeTimeout, "The crawl should not stop before the timeout");
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
            Assert.IsFalse(result.ErrorOccurred, "A timeout is not an error");
        }

        [TestMethod]
        public async Task CrawlBag_IsSetOnCrawlContext()
        {
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHtmlParser.Setup(f => f.GetLinks(It.IsAny<CrawledPage>())).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            CrawlContext actualCrawlContext = null;

            _unitUnderTest.PageCrawlCompleted += (s, e) => actualCrawlContext = e.CrawlContext;

            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
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
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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
            //One worker, so memory is read exactly once before the first page would be taken
            _dummyConfiguration.MaxConcurrentThreads = 1;
            _dummyConfiguration.MaxMemoryUsageInMb = 1;
            _fakeMemoryManager.Setup(f => f.GetCurrentUsageInMb()).Returns(2);
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            _fakeMemoryManager.Verify(f => f.GetCurrentUsageInMb(), Times.Exactly(2));

            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(1));
            Assert.AreEqual(0, _dummyScheduler.Count);
            Assert.IsTrue(result.ErrorOccurred);
            Assert.IsTrue(result.ErrorException is InsufficientMemoryException);
            Assert.AreEqual("Process is using [2mb] of memory which is above the max configured of [1mb] for site [http://a.com/]. This is configurable through the maxMemoryUsageInMb in app.conf or CrawlConfiguration.MaxMemoryUsageInMb.", result.ErrorException.Message);
            Assert.IsFalse(result.CrawlContext.IsCrawlStopRequested);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_LinkFoundOnPagesCrawledAtTheSameTime_IsCheckedOnce()
        {
            var page1Uri = new Uri("http://a.com/page1");
            var page2Uri = new Uri("http://a.com/page2");
            var sharedLink = new Uri("http://a.com/shared");

            using (var otherPageCompleted = new ManualResetEventSlim())
            {
                _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri)))
                    .Returns(new List<HyperLink> { new HyperLink { HrefValue = page1Uri }, new HyperLink { HrefValue = page2Uri } });
                _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri != _rootUri)))
                    .Returns(new List<HyperLink> { new HyperLink { HrefValue = sharedLink } });
                _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()))
                    .Returns<Uri, Func<CrawledPage, CrawlDecision>, CancellationToken>((uri, shouldDownloadContent, cancellationToken) => Task.FromResult(new CrawledPage(uri)));
                _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
                //The first page to decide on the shared link is held until the other page has completed, so the other page always
                //reaches the shared link while that decision is being made. A held page cannot complete, so the page that completes
                //is always the other one.
                var sharedLinkDecisions = 0;
                _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == sharedLink), It.IsAny<CrawlContext>()))
                    .Callback(() =>
                    {
                        if (Interlocked.Increment(ref sharedLinkDecisions) == 1)
                            otherPageCompleted.Wait(TestContext.CancellationToken);
                    })
                    .Returns(new CrawlDecision { Allow = false, Reason = "Not crawled" });
                _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
                _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
                _unitUnderTest.PageCrawlCompleted += (sender, args) =>
                {
                    if (args.CrawledPage.Uri == page1Uri || args.CrawledPage.Uri == page2Uri)
                        otherPageCompleted.Set();
                };

                await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

                _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == sharedLink), It.IsAny<CrawlContext>()), Times.Once());
            }
        }

        [TestMethod]
        public async Task Crawl_MaxConcurrentThreads_LimitsThePagesProcessedAtTheSameTime()
        {
            const int maxConcurrentPages = 3;
            _dummyConfiguration.MaxConcurrentThreads = maxConcurrentPages;
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            var links = new List<HyperLink>();
            for (var i = 0; i < 10; i++)
                links.Add(new HyperLink { HrefValue = new Uri(_rootUri, "page" + i) });
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri))).Returns(links);
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri != _rootUri))).Returns(new List<HyperLink>());
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });

            //Every page but the root is held until the test releases it. A page counts as in progress until its worker has the response.
            var startedRequests = new BlockingCollection<TaskCompletionSource<bool>>();
            var inProgress = 0;
            var maxInProgress = 0;
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CrawledPage(_rootUri)));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u != _rootUri), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()))
                .Returns<Uri, Func<CrawledPage, CrawlDecision>, CancellationToken>((uri, shouldDownloadContent, cancellationToken) =>
                {
                    var current = Interlocked.Increment(ref inProgress);
                    int max;
                    while (current > (max = Volatile.Read(ref maxInProgress)) && Interlocked.CompareExchange(ref maxInProgress, current, max) != max) { }

                    var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    startedRequests.Add(release, TestContext.CancellationToken);
                    return release.Task.ContinueWith(t =>
                    {
                        Interlocked.Decrement(ref inProgress);
                        return new CrawledPage(uri);
                    }, TaskScheduler.Default);
                });

            using (startedRequests)
            {
                var crawl = _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

                //Page i is released only once the limit is reached again (or every page has started), so the limit is always reached
                var started = new List<TaskCompletionSource<bool>>();
                for (var i = 0; i < links.Count; i++)
                {
                    while (started.Count < Math.Min(i + maxConcurrentPages, links.Count))
                        started.Add(startedRequests.Take(TestContext.CancellationToken));
                    started[i].SetResult(true);
                }

                var result = await crawl;

                Assert.AreEqual(links.Count + 1, result.CrawlContext.CrawledCount);
            }

            Assert.AreEqual(maxConcurrentPages, maxInProgress, "MaxConcurrentThreads pages should be processed at the same time, and never more");
        }

        [TestMethod]
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

            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(homePage));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(fakeLink1, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(page1));
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(fakeLink2, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(page2));
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == fakeLink3), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision {Allow = true});

            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);
            await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            // The links should be checked only one time, so ShouldCrawlPage should be called only 4 times.
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>()), Times.Exactly(4));
            _fakeHtmlParser.VerifyAll();
            _fakeCrawlDecisionMaker.VerifyAll();
        }






        [TestMethod]
        public async Task Crawl_CanExtractRetryAfterTimeFromHeaders()
        {
            await AssertRetryWaitsForRetryAfter(time => new RetryConditionHeaderValue(TimeSpan.FromSeconds(1)), 1000);
        }

        [TestMethod]
        public async Task Crawl_CanExtractRetryAfterDateFromHeaders()
        {
            //A date three seconds after the request, on the crawler's clock
            await AssertRetryWaitsForRetryAfter(time => new RetryConditionHeaderValue(time.GetUtcNow().AddSeconds(3)), 3000);
        }

        //The first request answers 503 with the given Retry-After; the retry must still be waiting 1 ms before the expected time and happen at it.
        //Runs on a manual clock, so the result does not depend on how long anything takes.
        private async Task AssertRetryWaitsForRetryAfter(Func<ManualTimeProvider, RetryConditionHeaderValue> retryAfter, int expectedWaitInMilliseconds)
        {
            var time = new ManualTimeProvider();
            _unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object, time);
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
            httpResponseMessage.Headers.RetryAfter = retryAfter(time);
            var page = new CrawledPage(_rootUri)
            {
                HttpRequestException = new HttpRequestException("aaa"),
                HttpRequestMessage = httpRequestMessage,
                HttpResponseMessage = httpResponseMessage
            };
            var requests = 0;
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()))
                .Callback(() => Interlocked.Increment(ref requests))
                .Returns(Task.FromResult(page));

            var retryWaiting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            time.TimerScheduled += () => retryWaiting.TrySetResult(true);

            using (TestContext.CancellationToken.Register(() => retryWaiting.TrySetCanceled(TestContext.CancellationToken)))
            {
                var crawl = _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);
                await retryWaiting.Task;

                time.Advance(TimeSpan.FromMilliseconds(expectedWaitInMilliseconds - 1));
                Assert.AreEqual(1, Volatile.Read(ref requests), "The retry should still be waiting");

                time.Advance(TimeSpan.FromMilliseconds(1));
                await crawl;
            }

            Assert.AreEqual(2, requests);
            Assert.AreEqual(expectedWaitInMilliseconds / 1000.0, page.RetryAfter);
            _fakeCrawlDecisionMaker.Verify(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task Crawl_LinkThatIsNotAValidUri_IsSkippedAndTheCrawlContinues()
        {
            var invalidLink = new Uri("http://a.com/invalid");
            var validLink = new Uri("http://a.com/valid");
            SetUpCrawlOfLinks(invalidLink, validLink);
            //Some uris parse but throw on use, which the crawler meets when it decides whether the link is internal
            _unitUnderTest.IsInternalUriDecisionMaker = (uri, rootUri) => uri == invalidLink ? throw new UriFormatException("Invalid host") : uri.Authority == rootUri.Authority;

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.IsFalse(result.ErrorOccurred);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(validLink, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(invalidLink, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [TestMethod]
        public async Task Crawl_DecisionMakerThrowsForALink_CrawlStopsWithTheError()
        {
            var link = new Uri("http://a.com/page");
            SetUpCrawlOfLinks(link);
            var error = new InvalidOperationException("Decision maker failed");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == link), It.IsAny<CrawlContext>())).Throws(error);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.AreSame(error, result.ErrorException);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_RedirectToUriThatIsNotValid_IsNotFollowedAndTheCrawlContinues()
        {
            var redirectingPage = new Uri("http://a.com/redirecting");
            var redirectTarget = new Uri("http://a.com/target");
            SetUpCrawlOfLinks(redirectingPage);
            SetUpRedirect(redirectingPage, redirectTarget);
            _unitUnderTest.IsInternalUriDecisionMaker = (uri, rootUri) => uri == redirectTarget ? throw new UriFormatException("Invalid host") : uri.Authority == rootUri.Authority;

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.IsFalse(result.ErrorOccurred);
            Assert.IsFalse(result.CrawlContext.IsCrawlHardStopRequested);
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(redirectTarget, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [TestMethod]
        public async Task Crawl_DecisionMakerThrowsForARedirectTarget_CrawlStopsWithTheError()
        {
            var redirectingPage = new Uri("http://a.com/redirecting");
            var redirectTarget = new Uri("http://a.com/target");
            SetUpCrawlOfLinks(redirectingPage);
            SetUpRedirect(redirectingPage, redirectTarget);
            var error = new InvalidOperationException("Decision maker failed");
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.Is<PageToCrawl>(p => p.Uri == redirectTarget), It.IsAny<CrawlContext>())).Throws(error);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.AreSame(error, result.ErrorException);
            Assert.IsTrue(result.CrawlContext.IsCrawlHardStopRequested);
        }

        [TestMethod]
        public async Task Crawl_RedirectWithoutLocationHeader_IsNotFollowed()
        {
            //With recrawling enabled, following the missing location (which resolves to the site's base uri, the root here) would crawl the root again
            _dummyConfiguration.IsUriRecrawlingEnabled = true;
            var redirectingPage = new Uri("http://a.com/redirecting");
            SetUpCrawlOfLinks(redirectingPage);
            SetUpRedirect(redirectingPage, null);

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

            Assert.IsFalse(result.ErrorOccurred);
            _fakeHttpRequester.Verify(f => f.MakeRequestAsync(_rootUri, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()), Times.Once());
            Assert.AreEqual(2, result.CrawlContext.CrawledCount);
        }

        [TestMethod]
        public async Task Crawl_RootRedirectWithoutLocationHeader_KeepsTheRoot()
        {
            var root = new Uri("http://a.com/start");
            SetUpCrawlOfLinks();
            SetUpRedirect(root, null);

            var result = await _unitUnderTest.CrawlAsync(root, TestContext.CancellationToken);

            Assert.IsFalse(result.ErrorOccurred);
            Assert.AreEqual(root, result.CrawlContext.RootUri);
            Assert.AreEqual(root, result.CrawlContext.OriginalRootUri);
        }

        //The root links to the given pages, which have no links themselves; every page is allowed and answers 200
        private void SetUpCrawlOfLinks(params Uri[] links)
        {
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPage(It.IsAny<PageToCrawl>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldCrawlPageLinks(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = true });
            _fakeCrawlDecisionMaker.Setup(f => f.ShouldRecrawlPage(It.IsAny<CrawledPage>(), It.IsAny<CrawlContext>())).Returns(new CrawlDecision { Allow = false });
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri == _rootUri))).Returns(links.Select(link => new HyperLink { HrefValue = link }).ToList());
            _fakeHtmlParser.Setup(f => f.GetLinks(It.Is<CrawledPage>(p => p.Uri != _rootUri))).Returns(new List<HyperLink>());
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()))
                .Returns<Uri, Func<CrawledPage, CrawlDecision>, CancellationToken>((uri, shouldDownloadContent, cancellationToken) => Task.FromResult(new CrawledPage(uri)));
        }

        //The page answers 302 to the target (no Location header if null), and the crawler follows redirects itself
        private void SetUpRedirect(Uri page, Uri target)
        {
            _dummyConfiguration.IsHttpRequestAutoRedirectsEnabled = false;
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(page, It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.Redirect) { RequestMessage = new HttpRequestMessage { RequestUri = page } };
                    response.Headers.Location = target;
                    return Task.FromResult(new CrawledPage(page) { HttpRequestMessage = response.RequestMessage, HttpResponseMessage = response });
                });
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
            _fakeHttpRequester.Setup(f => f.MakeRequestAsync(It.IsAny<Uri>(), It.IsAny<Func<CrawledPage, CrawlDecision>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(page));

            var result = await _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken);

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

        [TestMethod]
        public void Dispose_DoesNotDisposeImplementationsPassedIn()
        {
            var fakeScheduler = new Mock<IScheduler>();
            _dummyConfiguration.MaxMemoryUsageInMb = 1000;
            var unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, fakeScheduler.Object, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            unitUnderTest.Dispose();

            fakeScheduler.Verify(f => f.Dispose(), Times.Never);
            _fakeHttpRequester.Verify(f => f.Dispose(), Times.Never);
            _fakeMemoryManager.Verify(f => f.Dispose(), Times.Never);
        }

        //The obsolete constructor still works: the thread manager passed in only sets the concurrency, and is neither used nor disposed
#pragma warning disable CS0618
        [TestMethod]
        public void ObsoleteConstructorWithThreadManager_DoesNotUseOrDisposeIt()
        {
            var fakeThreadManager = new Mock<IThreadManager>();
            fakeThreadManager.Setup(f => f.MaxThreads).Returns(3);
            var unitUnderTest = new PoliteWebCrawler(_dummyConfiguration, _fakeCrawlDecisionMaker.Object, fakeThreadManager.Object, _dummyScheduler, _fakeHttpRequester.Object, _fakeHtmlParser.Object, _fakeMemoryManager.Object, _fakeDomainRateLimiter.Object, _fakeRobotsDotTextFinder.Object);

            unitUnderTest.Dispose();

            fakeThreadManager.Verify(f => f.DoWork(It.IsAny<Action>()), Times.Never);
            fakeThreadManager.Verify(f => f.Dispose(), Times.Never);
        }
#pragma warning restore CS0618

        [TestMethod]
        public async Task CrawlAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            _unitUnderTest.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => _unitUnderTest.CrawlAsync(_rootUri, TestContext.CancellationToken));
        }

        [TestMethod]
        public void Dispose_DefaultImplementations_CalledTwice_DoesNotThrow()
        {
            _dummyConfiguration.MaxMemoryUsageInMb = 1000;
            _dummyConfiguration.CrawlTimeoutSeconds = 10;
            var unitUnderTest = new PoliteWebCrawler(_dummyConfiguration);

            unitUnderTest.Dispose();
            unitUnderTest.Dispose();
        }

        //Holds the first call of a crawler event handler until released, so a test can check that the crawl waits for its handlers
        private sealed class BlockingHandler : IDisposable
        {
            readonly ManualResetEventSlim _blocked = new ManualResetEventSlim();
            readonly ManualResetEventSlim _released = new ManualResetEventSlim();
            readonly CancellationToken _cancellationToken;
            int _calls;
            int _finishedCalls;

            public BlockingHandler(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public int FinishedCalls => Volatile.Read(ref _finishedCalls);

            public void Handle()
            {
                if (Interlocked.Increment(ref _calls) == 1)
                {
                    _blocked.Set();
                    _released.Wait(_cancellationToken);
                }
                Interlocked.Increment(ref _finishedCalls);
            }

            public void WaitUntilBlocked() => _blocked.Wait(_cancellationToken);

            public void Release() => _released.Set();

            public void Dispose()
            {
                _blocked.Dispose();
                _released.Dispose();
            }
        }
    }
}
