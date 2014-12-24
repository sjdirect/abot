using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.Net;
using System.Text;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class WebContentExtractorTest
    {
        WebContentExtractor _uut;
        Uri _utf8 = new Uri("http://localhost:1111/");
        Uri _japan = new Uri("http://aaa.jp");

        [SetUp]
        public void Setup()
        {
            _uut = new WebContentExtractor();
        }

        [Test]
        public void GetContent_Utf8()
        {
            PageContent result = null;
            using (WebResponse response = GetWebStream(_utf8))
            {
                result = _uut.GetContent(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("utf-8", result.Charset);
            Assert.AreEqual(Encoding.UTF8, result.Encoding);
            Assert.IsTrue(result.Text.StartsWith("<!DOCTYPE html>\r\n<html>\r\n<head>\r\n"));

        }

        [Test]
        public void GetContent_NonUtf8()
        {
            PageContent result = null;
            using (WebResponse response = GetWebStream(_japan))
            {
                result = _uut.GetContent(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreNotEqual(Encoding.UTF8, result.Encoding);
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        private WebResponse GetWebStream(Uri uri)
        {
            WebRequest request = WebRequest.Create(uri);

            return request.GetResponse();
        }
    }
}
