using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public abstract class PagesToCrawlRepositoryTest
    {
        protected IPagesToCrawlRepository _unitUnderTest;
        PageToCrawl _page1;
        PageToCrawl _page2;

        public abstract IPagesToCrawlRepository GetInstance();

        [TestInitialize]
        public void SetUp()
        {
            _unitUnderTest = GetInstance();
            _page1 = new PageToCrawl(new Uri("http://a.com"));
            _page2 = new PageToCrawl(new Uri("http://b.com"));
        }

        [TestMethod]
        public void Add_SinglePage_AddsToQueue()
        {
            Assert.AreEqual(0, _unitUnderTest.Count());

            _unitUnderTest.Add(_page1);

            Assert.AreEqual(1, _unitUnderTest.Count());
        }

        [TestMethod]
        public void Add_MultiplePages_AddsToQueue()
        {
            _unitUnderTest.Add(_page1);
            _unitUnderTest.Add(_page2);

            Assert.AreEqual(2, _unitUnderTest.Count());
        }

        [TestMethod]
        public void Add_AddingDuplicates_AddsAll()
        {
            _unitUnderTest.Add(_page1);
            _unitUnderTest.Add(_page1);
            _unitUnderTest.Add(_page1);
            _unitUnderTest.Add(_page1);

            Assert.AreEqual(4, _unitUnderTest.Count());
        }

        [TestMethod]
        public void GetNext_MultiplePages_ReturnsInFifoOrder()
        {
            var page3 = new PageToCrawl(new Uri("http://abc/"));
            var page4 = new PageToCrawl(new Uri("http://abcd/"));
            
            _unitUnderTest.Add(_page1);
            _unitUnderTest.Add(_page2);
            _unitUnderTest.Add(page3);
            _unitUnderTest.Add(page4);

            var result1 = _unitUnderTest.GetNext();
            var result2 = _unitUnderTest.GetNext();
            var result3 = _unitUnderTest.GetNext();
            var result4 = _unitUnderTest.GetNext();
            var result5 = _unitUnderTest.GetNext();//should be null

            Assert.AreSame(_page1, result1);
            Assert.AreSame(_page2, result2);
            Assert.AreSame(page3, result3);
            Assert.AreSame(page4, result4);
            Assert.IsNull(result5);
        }

        [TestMethod]
        public void GetNext_NoPages_ReturnsNull()
        {
            Assert.IsNull(_unitUnderTest.GetNext());
        }

        [TestMethod]
        public void Count()
        {
            //This method is tested by most of the other methods in this class
        }


        [TestMethod]
        public void Clear_NoPages_DoesNothing()
        {
            _unitUnderTest.Clear();

            Assert.AreEqual(0, _unitUnderTest.Count());
        }
    }
}
