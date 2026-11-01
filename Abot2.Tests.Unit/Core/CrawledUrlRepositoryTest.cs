using Abot2.Core;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
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

        //Every implementation guarantees this. Whether AddIfNew reports each unique uri as new is only guaranteed by
        //exact implementations: hash-based ones can, by design, see two different uris as the same.
        [TestMethod]
        public void NoFalseNegativesTest()
        {
            // create input collection
            var inputs = GenerateUriList(10000);

            // instantiate filter and populate it with the inputs
            using (var uut = GetInstance())
            {
                foreach (var input in inputs)
                    uut.AddIfNew(input);

                //Everything that was added must be reported by Contains()
                foreach (var input in inputs)
                {
                    if (!uut.Contains(input))
                        Assert.Fail($"False negative: {input}");
                }
            }
        }

        //The same uris on every run, so results are reproducible
        protected static List<Uri> GenerateUriList(int capacity)
        {
            var uris = new List<Uri>(capacity);
            for (var i = 0; i < capacity; i++)
            {
                uris.Add(new Uri($"http://site{i}.com/page{i}"));
            }
            return uris;
        }
    }
}
