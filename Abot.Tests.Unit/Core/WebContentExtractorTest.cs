using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class WebContentExtractorTest
    {
        WebContentExtractor _uut;
        Uri _utf8 = new Uri("http://localhost.fiddler:1111/");
        Uri _japan = new Uri("http://aaa.jp");
        Uri _japanMetaSingleQuotes = new Uri("http://aaa2.jp");
        Uri _japanMetaDoubleQuotesAndClose = new Uri("http://aaa3.jp");
        Uri _japanMetaSingleQuotesAndClose = new Uri("http://aaa4.jp");

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
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public void GetContent_MetaSingleQuotes_NonUtf8()
        {
            PageContent result = null;
            using (WebResponse response = GetWebStream(_japanMetaSingleQuotes))
            {
                result = _uut.GetContent(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public void GetContent_MetaDoubleQuotesAndClose_NonUtf8()
        {
            PageContent result = null;
            using (WebResponse response = GetWebStream(_japanMetaDoubleQuotesAndClose))
            {
                result = _uut.GetContent(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public void GetContent_MetaSingleQuotesAndClose_NonUtf8()
        {
            PageContent result = null;
            using (WebResponse response = GetWebStream(_japanMetaSingleQuotesAndClose))
            {
                result = _uut.GetContent(response);
            }

            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual("Shift_JIS", result.Charset);
            Assert.AreEqual("System.Text.DBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        [Test]
        public void GetContent_Cp1251_ConvertsToWindows1251()
        {
            WebRequest.RegisterPrefix("test", new TestWebRequestCreate());
            TestWebRequest request = TestWebRequestCreate.CreateTestRequest("<meta http-equiv=Content-Type content=\"text/html; charset=cp1251\">");
            var response = request.GetResponse();
             
            PageContent result = _uut.GetContent(response);

            Assert.IsNotNull(result.Bytes);
            Assert.AreEqual(66, result.Bytes.Length);
            Assert.AreEqual("windows-1251", result.Charset);
            Assert.AreEqual("System.Text.SBCSCodePageEncoding", result.Encoding.ToString());
            Assert.IsTrue(result.Text.StartsWith("<meta http-equiv="));
        }

        private WebResponse GetWebStream(Uri uri)
        {
            WebRequest request = WebRequest.Create(uri);

            return request.GetResponse();
        }
    }


    class TestWebRequestCreate : IWebRequestCreate
    {
        static WebRequest nextRequest;
        static object lockObject = new object();

        static public WebRequest NextRequest
        {
            get { return nextRequest; }
            set
            {
                lock (lockObject)
                {
                    nextRequest = value;
                }
            }
        }

        /// <summary>See <see cref="IWebRequestCreate.Create"/>.</summary>
        public WebRequest Create(Uri uri)
        {
            return nextRequest;
        }

        /// <summary>Utility method for creating a TestWebRequest and setting 
        /// it to be the next WebRequest to use.</summary>
        /// <param name="response">The response the TestWebRequest will return.</param>
        public static TestWebRequest CreateTestRequest(string response)
        {
            TestWebRequest request = new TestWebRequest(response);
            NextRequest = request;
            return request;
        }
    }

    class TestWebRequest : WebRequest
    {
        MemoryStream requestStream = new MemoryStream();
        MemoryStream responseStream;

        public override string Method { get; set; }
        public override string ContentType { get; set; }
        public override long ContentLength { get; set; }

        /// <summary>Initializes a new instance of <see cref="TestWebRequest"/> 
        /// with the response to return.</summary>
        public TestWebRequest(string response)
        {
            responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response));
        }

        /// <summary>Returns the request contents as a string.</summary>
        public string ContentAsString()
        {
            return System.Text.Encoding.UTF8.GetString(requestStream.ToArray());
        }

        /// <summary>See <see cref="WebRequest.GetRequestStream"/>.</summary>
        public override Stream GetRequestStream()
        {
            return requestStream;
        }

        /// <summary>See <see cref="WebRequest.GetResponse"/>.</summary>
        public override WebResponse GetResponse()
        {
            return new TestWebReponse(responseStream);
        }
    }

    class TestWebReponse : WebResponse
    {
        Stream responseStream;
        WebHeaderCollection headers;
        public override WebHeaderCollection Headers { get { return headers; } }

        /// <summary>Initializes a new instance of <see cref="TestWebReponse"/> 
        /// with the response stream to return.</summary>
        public TestWebReponse(Stream responseStream)
        {
            this.responseStream = responseStream;
            headers = new WebHeaderCollection();
        }

        /// <summary>See <see cref="WebResponse.GetResponseStream"/>.</summary>
        public override Stream GetResponseStream()
        {
            return responseStream;
        }
    }
}
