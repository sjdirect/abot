using Abot.Core;
using Abot.Poco;
using Commoner.Core.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public abstract class HashGeneratorTest
    {
        IHashGenerator _unitUnderTest;

        protected abstract IHashGenerator GetInstance();

        [SetUp]
        public void Setup()
        {
            _unitUnderTest = GetInstance();
        }

        [Test]
        public void GenerateHash_NoCollisions()
        {
            List<Uri> randomUris = GenerateRandomDataList(10000);
            Dictionary<string, byte[]> hashes = new Dictionary<string, byte[]>();

            try
            {
                foreach (Uri input in randomUris)
                {
                    hashes.Add(input.AbsoluteUri, _unitUnderTest.GenerateHash(input.AbsoluteUri));
                }
            }
            catch (Exception e)
            {
                Assert.Fail();
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
