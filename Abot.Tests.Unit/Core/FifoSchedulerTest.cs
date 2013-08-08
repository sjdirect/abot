using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class FifoSchedulerTest
    {
        FifoScheduler _unitUnderTest;

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new FifoScheduler();
        }

        [Test]
        public void Add_ValidPageToCrawl_IsAdded()
        {
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));

            Assert.AreEqual(1, _unitUnderTest.Count);
        }

        [Test]
        public void Add_IsUriRecrawlingIsFalse_DuplicateNotAdded()
        {
            _unitUnderTest = new FifoScheduler(false);//this is the default
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));

            Assert.AreEqual(1, _unitUnderTest.Count);
        }

        [Test]
        public void Add_IEnumerableParam_IsUriRecrawlingIsFalse_DuplicateNotAdded()
        {
            _unitUnderTest = new FifoScheduler(false);//this is the default

            _unitUnderTest.Add(new List<PageToCrawl> { new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")) });

            Assert.AreEqual(1, _unitUnderTest.Count);
        }

        [Test]
        public void Add_IsUriRecrawlingIsTrue_DuplicateAdded()
        {
            _unitUnderTest = new FifoScheduler(true);
            
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));

            Assert.AreEqual(3, _unitUnderTest.Count);
        }

        [Test]
        public void Add_IEnumerableParam_IsUriRecrawlingIsTrue_DuplicateAdded()
        {
            _unitUnderTest = new FifoScheduler(true);//this is the default

            _unitUnderTest.Add(new List<PageToCrawl> { new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")), new PageToCrawl(new Uri("http://a.com/")) });

            Assert.AreEqual(3, _unitUnderTest.Count);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullPage()
        {
            PageToCrawl nullPage = null;
            _unitUnderTest.Add(nullPage);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_IEnumerableParam_NullPages()
        {
            IEnumerable<PageToCrawl> nullPages = null;
            _unitUnderTest.Add(nullPages);
        }

        [Test]
        public void GetNext()
        {
            Assert.AreEqual(0, _unitUnderTest.Count);

            PageToCrawl page1 = new PageToCrawl(new Uri("http://a.com/1"));
            PageToCrawl page2 = new PageToCrawl(new Uri("http://a.com/2"));
            PageToCrawl page3 = new PageToCrawl(new Uri("http://a.com/3"));

            _unitUnderTest.Add(page1);
            _unitUnderTest.Add(page2);
            _unitUnderTest.Add(page3);

            Assert.AreEqual(3, _unitUnderTest.Count);
            Assert.AreEqual(page1.Uri, _unitUnderTest.GetNext().Uri);
            Assert.AreEqual(page2.Uri, _unitUnderTest.GetNext().Uri);
            Assert.AreEqual(page3.Uri, _unitUnderTest.GetNext().Uri);
            Assert.AreEqual(0, _unitUnderTest.Count);
        }

        [Test]
        public void Clear_RemovesAllPrevious()
        {
            _unitUnderTest = new FifoScheduler();
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://a.com/")));
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://b.com/")));
            _unitUnderTest.Add(new PageToCrawl(new Uri("http://c.com/")));

            _unitUnderTest.Clear();

            Assert.AreEqual(0, _unitUnderTest.Count);
        }
    }
}
