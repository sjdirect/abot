using Abot.Core;
using Commoner.Core.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public abstract class CrawledUrlRepositoryTest
    {
        ICrawledUrlRepository _unitUnderTest;
        Uri _uri1;
        Uri _uri2;

        public abstract ICrawledUrlRepository GetInstance();

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = GetInstance();
            _uri1 = new Uri("http://a.com");
            _uri2 = new Uri("http://b.com");
        }

        [Test]
        public void AddIfNew_AddingUniqueUri_ReturnsTrue()
        {
            Assert.IsTrue(_unitUnderTest.AddIfNew(_uri1));
        }

        [Test]
        public void AddIfNew_AddingDuplicateUri_ReturnsFalse()
        {
            _unitUnderTest.AddIfNew(_uri1);//Add first time

            bool result = _unitUnderTest.AddIfNew(_uri1);

            Assert.IsFalse(result);
        }


        [Test]
        public void Contains_NonExistent_ReturnsFalse()
        {
            Assert.IsFalse(_unitUnderTest.Contains(_uri1));
            Assert.IsFalse(_unitUnderTest.Contains(_uri2));

            _unitUnderTest.AddIfNew(_uri1);

            Assert.IsFalse(_unitUnderTest.Contains(_uri2));
        }


        [Test]
        public void Dispose_SetsInnerCollectionToNull()
        {
            Assert.IsNotNull(ValueHelper.GetFieldValue(_unitUnderTest, "_urlRepository"));
            
            _unitUnderTest.Dispose();

            Assert.IsNull(ValueHelper.GetFieldValue(_unitUnderTest, "_urlRepository"));
        }


        [Test]
        public void NoFalseNegativesTest()
        {
            // create input collection
            List<Uri> inputs = GenerateRandomDataList(10000);

            // instantiate filter and populate it with the inputs
            ICrawledUrlRepository uut = GetInstance();
            foreach (Uri input in inputs)
                Assert.IsTrue(uut.AddIfNew(input));

            // check for each input. if any are missing, the test failed
            foreach (Uri input in inputs)
            {
                if (uut.Contains(input) == false)
                    Assert.Fail("False negative: {0}", input);
            }
        }

        private List<Uri> GenerateRandomDataList(int capacity)
        {
            List<Uri> uris = new List<Uri>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                uris.Add(new Uri("http://" + Guid.NewGuid().ToString()));
            }
            return uris;
        }
    }
}
