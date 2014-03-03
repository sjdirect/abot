using Abot.Core;
using Abot.Poco;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class HttpRequestEngineTest
    {
        HttpRequestEngine _uut;
        Mock<IPageRequester> _fakePageRequester;
        PageToCrawl _page1;
        PageToCrawl _page2;
        CrawledPage _cpage1;
        CrawledPage _cpage2;
        CrawlContext _context;

        [SetUp]
        public void Setup()
        {
            _page1 = new PageToCrawl(new Uri("http://a.com/a"));
            _page2 = new PageToCrawl(new Uri("http://a.com/b"));
            _cpage1 = new CrawledPage(_page1.Uri);
            _cpage2 = new CrawledPage(_page2.Uri);

            _fakePageRequester = new Mock<IPageRequester>();

            _fakePageRequester.Setup(f => f.MakeRequest(_page1.Uri)).Returns(_cpage1);
            _fakePageRequester.Setup(f => f.MakeRequest(_page2.Uri)).Returns(_cpage2);

            _context = new CrawlContext { CrawlConfiguration = new CrawlConfiguration() };
            _context.PagesToCrawl.Add(_page1);
            _context.PagesToCrawl.Add(_page2);
            
            _uut = new HttpRequestEngine(_context.CrawlConfiguration, new TaskThreadManager(1), _fakePageRequester.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (_uut != null)
                _uut.Stop();
        }


        [Test]
        public void Constructor_InstantiatesDefaultImpls()
        {
            HttpRequestEngine uut = new HttpRequestEngine();

            Assert.IsTrue(uut.ThreadManager is TaskThreadManager);
            Assert.IsTrue(uut.PageRequester is PageRequester);
        }


        [Test]
        public void Start_PagesAreRequested()
        {
            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(500);

            _fakePageRequester.VerifyAll();
        }

        [Test]
        public void Start_SynchronousEventsFire()
        {
            int pageRequestStartingCount = 0;
            _uut.PageRequestStarting += (a, b) => Interlocked.Increment(ref pageRequestStartingCount);

            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompleted += (a, b) => Interlocked.Increment(ref pageRequestCompletedCount);

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(100);

            Assert.AreEqual(2, pageRequestStartingCount);
            Assert.AreEqual(2, pageRequestCompletedCount);
        }

        [Test]
        public void Start_AsyncEventsFire()
        {
            int pageRequestStartingAsyncCount = 0;
            _uut.PageRequestStartingAsync += (a, b) => Interlocked.Increment(ref pageRequestStartingAsyncCount);

            int pageRequestCompletedAsyncCount = 0;
            _uut.PageRequestCompletedAsync += (a, b) => Interlocked.Increment(ref pageRequestCompletedAsyncCount);

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(500);

            Assert.AreEqual(2, pageRequestStartingAsyncCount);
            Assert.AreEqual(2, pageRequestCompletedAsyncCount);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Start_NullCrawlContext()
        {
            _uut.Start(null, null);
        }


        [Test]
        public void Stop_NoPagesAreRequested()
        {
            _uut.PageRequestStarting += (a, b) => System.Threading.Thread.Sleep(3000);
            _uut.Start(_context, null);

            _uut.Stop();

            Assert.IsTrue(_uut.IsDone);
            _fakePageRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Never);
        }

        [Test]
        public void Stop_NoEventsAreFired()
        {
            int eventCount = 0;
            _uut.PageRequestStarting += (a, b) => 
            {
                System.Threading.Thread.Sleep(3000);
                Interlocked.Increment(ref eventCount);
            };
            _uut.PageRequestCompleted += (a, b) => 
            {
                System.Threading.Thread.Sleep(3000);
                Interlocked.Increment(ref eventCount);
            };
            _uut.Start(_context, null);

            _uut.Stop();

            Assert.IsTrue(_uut.IsDone);
            _fakePageRequester.Verify(f => f.MakeRequest(It.IsAny<Uri>()), Times.Never);
        }


        [Test]
        public void IsDone_IsDone_ReturnsTrue()
        {
            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            Assert.IsTrue(_uut.IsDone);
        }

        [Test]
        public void IsDone_IsNotDone_ReturnsFalse()
        {
            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();

            Assert.IsFalse(_uut.IsDone);
        }


        [Test]
        public void PageSizeAboveMax_CompleteEventDoesNotFireForThatPage()
        {
            //Arrange
            _context.PagesToCrawl = new BlockingCollection<PageToCrawl>();
            _context.PagesToCrawl.Add(_page1);
            _context.CrawlConfiguration.MaxPageSizeInBytes = 1;
            
            _cpage1.Content = new PageContent { Bytes = Encoding.ASCII.GetBytes("More Than 1 Byte!!!!!") };

            int pageRequestStartingCount = 0;
            _uut.PageRequestStarting += (a, b) => Interlocked.Increment(ref pageRequestStartingCount);

            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompleted += (a, b) => Interlocked.Increment(ref pageRequestCompletedCount);

            //Act
            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(500);

            //Assert
            Assert.IsTrue(_uut.IsDone);
            Assert.AreEqual(1, pageRequestStartingCount);
            Assert.AreEqual(0, pageRequestCompletedCount);
        }

        [Test]
        public void PersistsPageBagValues()
        {
            //Arrange
            _context.PagesToCrawl = new BlockingCollection<PageToCrawl>();
            _context.PagesToCrawl.Add(_page1);

            _page1.PageBag.Value1 = "aaa";
            _cpage1.PageBag.Value2 = "bbb";

            CrawledPage result = null;
            _uut.PageRequestCompleted += (a, b) =>
            {
                result = b.CrawledPage;
            };

            //Act
            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("aaa", result.PageBag.Value1);
            Assert.AreEqual("bbb", result.PageBag.Value2);
        }

        [Test]
        public void PageRequesterThrowsException_DoesNotFireCompleteEvent()
        {
            _fakePageRequester.Setup(f => f.MakeRequest(It.IsAny<Uri>())).Throws(new Exception("Oh no"));

            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompleted += (a, b) => Interlocked.Increment(ref pageRequestCompletedCount);

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            Assert.AreEqual(0, pageRequestCompletedCount);
        }

        [Test]
        public void PageRequestStarting_SubscriberThrowsException_StillFiresCompletedEvent()
        {
            _uut.PageRequestStarting += (a, b) => { throw new Exception("Oh no"); };

            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompleted += (a, b) => Interlocked.Increment(ref pageRequestCompletedCount);

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            Assert.AreEqual(2, pageRequestCompletedCount);
        }

        [Test]
        public void PageRequestCompleted_SubscriberThrowsException_DoesNotCrash()
        {
            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompleted += (a, b) => 
            {
                Interlocked.Increment(ref pageRequestCompletedCount);
                throw new Exception("Oh no"); 
            };

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            Assert.AreEqual(2, pageRequestCompletedCount);
        }

        [Test]
        public void PageRequestStartingAsyc_SubscriberThrowsException_DoesNotCrash()
        {
            _uut.PageRequestStartingAsync += (a, b) => { throw new Exception("Oh no"); };

            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompleted += (a, b) => Interlocked.Increment(ref pageRequestCompletedCount);

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            Assert.AreEqual(2, pageRequestCompletedCount);
        }

        [Test]
        public void PageRequestCompletedAsync_SubscriberThrowsException_DoesNotCrash()
        {
            int pageRequestCompletedCount = 0;
            _uut.PageRequestCompletedAsync += (a, b) =>
            {
                Interlocked.Increment(ref pageRequestCompletedCount);
                throw new Exception("Oh no");
            };

            _uut.Start(_context, null);
            _context.PagesToCrawl.CompleteAdding();
            System.Threading.Thread.Sleep(1000);

            Assert.AreEqual(2, pageRequestCompletedCount);
        }
    }
}
