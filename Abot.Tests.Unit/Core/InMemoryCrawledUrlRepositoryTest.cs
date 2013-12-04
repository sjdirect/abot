using Abot.Core;
using Commoner.Core.Testing;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class InMemoryCrawledUrlRepositoryTest
    {
        InMemoryCrawledUrlRepository _unitUnderTest;
        Uri _uri1;
        Uri _uri2;

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new InMemoryCrawledUrlRepository();
            _uri1 = new Uri("http://a.com");
            _uri2 = new Uri("http://b.com");
        }

        [Test]
        public void AddIfNew_AddingUniqueUri_ReturnsTrue()
        {
            Assert.IsTrue(_unitUnderTest.AddIfNew(_uri1));
        }

        [Test]
        public void AddIfNew_AddingDuplicateUniqueUri_ReturnsFalse()
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
    }
}
