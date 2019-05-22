using Abot2.Core;
using Abot2.Poco;
using Moq;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class SchedulerTest
    {
        Scheduler _unitUnderTest;
        Mock<ICrawledUrlRepository> _fakeCrawledUrlRepo;
        Mock<IPagesToCrawlRepository> _fakePagesToCrawlRepo;
        PageToCrawl _page;
        List<PageToCrawl> _pages;

        [TestInitialize]
        public void SetUp()
        {
            _page = new PageToCrawl { Uri = new Uri("http://a.com/") };
            _pages = new List<PageToCrawl> { new PageToCrawl { Uri = new Uri("http://a.com/") }, new PageToCrawl { Uri = new Uri("http://b.com/") } };
            _fakeCrawledUrlRepo = new Mock<ICrawledUrlRepository>();
            _fakePagesToCrawlRepo = new Mock<IPagesToCrawlRepository>();

            _unitUnderTest = new Scheduler(false, _fakeCrawledUrlRepo.Object, _fakePagesToCrawlRepo.Object);
        }

        [TestMethod]
        public void Constructor_NoParams()
        {
            Assert.IsNotNull(new Scheduler());
        }

        [TestMethod]
        public void Count_ReturnsPagesToCrawlRepoCount()
        {
            _fakePagesToCrawlRepo.Setup(f => f.Count()).Returns(11);

            var result = _unitUnderTest.Count;

            Assert.AreEqual(11, result);
            _fakePagesToCrawlRepo.VerifyAll();
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullPage()
        {
            PageToCrawl nullPage = null;
            _unitUnderTest.Add(nullPage);
        }

        [TestMethod]
        public void Add_UriRecrawlingDisabled_UrlHasNotBeenCrawled_AddsToBothRepos()
        {
            _fakeCrawledUrlRepo.Setup(f => f.AddIfNew(_page.Uri)).Returns(true);

            _unitUnderTest.Add(_page);

            _fakeCrawledUrlRepo.VerifyAll();
            _fakePagesToCrawlRepo.Verify(f => f.Add(_page));
        }

        [TestMethod]
        public void Add_UriRecrawlingDisabled_UrlHasBeenCrawled_DoesNotAddToPagesToCrawlRepo()
        {
            _fakeCrawledUrlRepo.Setup(f => f.AddIfNew(_page.Uri)).Returns(false);

            _unitUnderTest.Add(_page);

            _fakeCrawledUrlRepo.VerifyAll();
            _fakePagesToCrawlRepo.Verify(f => f.Add(_page), Times.Never());
        }

        [TestMethod]
        public void Add_UriRecrawlingDisabled_UrlHasBeenCrawled_IsRetry_AddsToBothRepos()
        {
            _page.IsRetry = true;
            _unitUnderTest = new Scheduler(false, _fakeCrawledUrlRepo.Object, _fakePagesToCrawlRepo.Object);

            _unitUnderTest.Add(_page);

            _fakeCrawledUrlRepo.Verify(f => f.AddIfNew(_page.Uri), Times.Never());
            _fakePagesToCrawlRepo.Verify(f => f.Add(_page));
        }

        [TestMethod]
        public void Add_UriRecrawlingEnabled_AddsToPagesToCrawlRepo()
        {
            _unitUnderTest = new Scheduler(true, _fakeCrawledUrlRepo.Object, _fakePagesToCrawlRepo.Object);

            _unitUnderTest.Add(_page);

            _fakeCrawledUrlRepo.Verify(f => f.AddIfNew(_page.Uri), Times.Never());
            _fakePagesToCrawlRepo.Verify(f => f.Add(_page));
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullPages()
        {
            IEnumerable<PageToCrawl> nullPages = null;
            _unitUnderTest.Add(nullPages);
        }

        [TestMethod]
        public void Add_UriRecrawlingDisabled_UrlCollectionHasNotBeenCrawled_AddsToBothRepos()
        {
            _fakeCrawledUrlRepo.Setup(f => f.AddIfNew(_pages[0].Uri)).Returns(true);
            _fakeCrawledUrlRepo.Setup(f => f.AddIfNew(_pages[1].Uri)).Returns(true);

            _unitUnderTest.Add(_pages);

            _fakeCrawledUrlRepo.VerifyAll();
            _fakePagesToCrawlRepo.Verify(f => f.Add(_pages[0]));
            _fakePagesToCrawlRepo.Verify(f => f.Add(_pages[1]));
        }

        [TestMethod]
        public void Add_UriRecrawlingDisabled_1UrlHasBeenCrawled_AddsOnly1ToPagesToCrawlRepo()
        {
            _fakeCrawledUrlRepo.Setup(f => f.AddIfNew(_pages[0].Uri)).Returns(true);
            _fakeCrawledUrlRepo.Setup(f => f.AddIfNew(_pages[1].Uri)).Returns(false);

            _unitUnderTest.Add(_pages);

            _fakeCrawledUrlRepo.VerifyAll();
            _fakePagesToCrawlRepo.Verify(f => f.Add(_pages[0]));
            _fakePagesToCrawlRepo.Verify(f => f.Add(_pages[1]), Times.Never());
        }

        [TestMethod]
        public void Add_UriRecrawlingEnabled_AddsBothToPagesToCrawlRepo()
        {
            _unitUnderTest = new Scheduler(true, _fakeCrawledUrlRepo.Object, _fakePagesToCrawlRepo.Object);

            _unitUnderTest.Add(_pages);

            _fakeCrawledUrlRepo.Verify(f => f.AddIfNew(It.IsAny<Uri>()), Times.Never());
            _fakePagesToCrawlRepo.Verify(f => f.Add(_pages[0]));
            _fakePagesToCrawlRepo.Verify(f => f.Add(_pages[1]));
        }


        [TestMethod]
        public void GetNext_ReturnsNextPageToCrawlFromRepo()
        {
            _fakePagesToCrawlRepo.Setup(f => f.GetNext()).Returns(_page);

            PageToCrawl result = _unitUnderTest.GetNext();

            _fakePagesToCrawlRepo.VerifyAll();
            Assert.AreSame(_page, result);
        }

        [TestMethod]
        public void Clear_ClearsPageToCrawlRepo()
        {
            _unitUnderTest.Clear();

            _fakePagesToCrawlRepo.Verify(f => f.Clear());
        }


        //[TestMethod]
        //public void Add_IEnumerableParam_IsUriRecrawlingIsFalse_DuplicateNotAdded()
        //{
        //    //_unitUnderTest = new FifoScheduler(false);//this is the default

        //    //_unitUnderTest.Add(new List<PageToCrawl> { new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")) });

        //    //Assert.AreEqual(1, _unitUnderTest.Count);
        //}

        //[TestMethod]
        //public void Add_IsUriRecrawlingIsTrue_DuplicateAdded()
        //{
        //    //_unitUnderTest = new FifoScheduler(true);
            
        //    //_unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
        //    //_unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
        //    //_unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));

        //    //Assert.AreEqual(3, _unitUnderTest.Count);
        //}

        //[TestMethod]
        //public void Add_IEnumerableParam_IsUriRecrawlingIsTrue_DuplicateAdded()
        //{
        //    //_unitUnderTest = new FifoScheduler(true);//this is the default

        //    //_unitUnderTest.Add(new List<PageToCrawl> { new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")) });

        //    //Assert.AreEqual(3, _unitUnderTest.Count);
        //}

        //[TestMethod]
        //[ExpectedException(typeof(ArgumentNullException))]
        //public void Add_NullPage()
        //{
        //    //PageToCrawl nullPage = null;
        //    //_unitUnderTest.Add(nullPage);
        //}

        //[TestMethod]
        //[ExpectedException(typeof(ArgumentNullException))]
        //public void Add_IEnumerableParam_NullPages()
        //{
        //    //IEnumerable<PageToCrawl> nullPages = null;
        //    //_unitUnderTest.Add(nullPages);
        //}

        //[TestMethod]
        //public void GetNext()
        //{
        //    //Assert.AreEqual(0, _unitUnderTest.Count);

        //    //PageToCrawl page1 = new PageToCrawl(new Uri("http://a.com/1"));
        //    //PageToCrawl page2 = new PageToCrawl(new Uri("http://a.com/2"));
        //    //PageToCrawl page3 = new PageToCrawl(new Uri("http://a.com/3"));

        //    //_unitUnderTest.Add(page1);
        //    //_unitUnderTest.Add(page2);
        //    //_unitUnderTest.Add(page3);

        //    //Assert.AreEqual(3, _unitUnderTest.Count);
        //    //Assert.AreEqual(page1.Uri, _unitUnderTest.GetNext().Uri);
        //    //Assert.AreEqual(page2.Uri, _unitUnderTest.GetNext().Uri);
        //    //Assert.AreEqual(page3.Uri, _unitUnderTest.GetNext().Uri);
        //    //Assert.AreEqual(0, _unitUnderTest.Count);
        //}

        //[TestMethod]
        //public void Clear_RemovesAllPrevious()
        //{
        //    //_unitUnderTest = new FifoScheduler();
        //    //_unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
        //    //_unitUnderTest.Add(new PageToCrawl(new Uri("http://b.com/")));
        //    //_unitUnderTest.Add(new PageToCrawl(new Uri("http://c.com/")));

        //    //_unitUnderTest.Clear();

        //    //Assert.AreEqual(0, _unitUnderTest.Count);
        //}
    }
}
