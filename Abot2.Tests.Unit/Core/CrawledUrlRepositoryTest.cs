using Abot2.Core;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public abstract class CrawledUrlRepositoryTest
    {
        protected ICrawledUrlRepository _unitUnderTest;
        Uri _uri1;
        Uri _uri2;

        public abstract ICrawledUrlRepository GetInstance();

        [TestInitialize]
        public void SetUp()
        {
            _unitUnderTest = GetInstance();
            _uri1 = new Uri("http://a.com");
            _uri2 = new Uri("http://b.com");
        }

        [TestCleanup]
        public void TearDown()
        {
            _unitUnderTest.Dispose();
        }

        [TestMethod]
        public void AddIfNew_AddingUniqueUri_ReturnsTrue()
        {
            Assert.IsTrue(_unitUnderTest.AddIfNew(_uri1));
        }

        [TestMethod]
        public void AddIfNew_AddingDuplicateUri_ReturnsFalse()
        {
            _unitUnderTest.AddIfNew(_uri1);//Add first time

            var result = _unitUnderTest.AddIfNew(_uri1);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Contains_NonExistent_ReturnsFalse()
        {
            Assert.IsFalse(_unitUnderTest.Contains(_uri1));
            Assert.IsFalse(_unitUnderTest.Contains(_uri2));

            _unitUnderTest.AddIfNew(_uri1);

            Assert.IsFalse(_unitUnderTest.Contains(_uri2));
        }

        [TestMethod]
        public void NoFalseNegativesTest()
        {
            // create input collection
            var inputs = GenerateRandomDataList(10000);

            // instantiate filter and populate it with the inputs
            using (var uut = GetInstance())
            {
                //If all were unique then they should all return "true" for Contains()
                foreach (var input in inputs)
                    Assert.IsTrue(uut.AddIfNew(input));

                //If all were added successfully then they should all return "true" for Contains()
                foreach (var input in inputs)
                {
                    if (!uut.Contains(input))
                        Assert.Fail("False negative: {0}", input);
                }
            }
        }

        private List<Uri> GenerateRandomDataList(int capacity)
        {
            var uris = new List<Uri>(capacity);
            for (var i = 0; i < capacity; i++)
            {
                uris.Add(new Uri("http://" + Guid.NewGuid().ToString()));
            }
            return uris;
        }
    }
}
