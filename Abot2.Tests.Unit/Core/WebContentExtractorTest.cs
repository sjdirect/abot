using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class WebContentExtractorTest
    {
        WebContentExtractor _uut;
        string _contentString = "<!DOCTYPE html>\r\n<html>\r\n<head>\r\nblahblahblah"; 

        const string JapaneseCharset = "Shift_JIS";
        const string JapaneseEncodingString = "System.Text.UTF8Encoding+UTF8EncodingSealed";

        [TestInitialize]
        public void Setup()
        {
            _uut = new WebContentExtractor();
        }

        [TestMethod]
        public async Task GetContent_NoCharsetDefinedInHeaderOrBody_ReturnsCompletePageContentObject()
        {
            PageContent result;
            var httpResponseMessage = new HttpResponseMessage()
            {
                Content = new FakeHttpContent(_contentString)
            };

            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, null);
            Assert.IsTrue(result.Text.StartsWith(_contentString));
        }

        [TestMethod]
        public async Task GetContent_CharsetDefinedInHeader_UTF16_ReturnsCompletePageContentObject()
        {
            var httpResponseMessage = new HttpResponseMessage()
            {
                RequestMessage = new HttpRequestMessage(),
                Content = new FakeHttpContent(_contentString)
            };
            httpResponseMessage.Content.Headers.Add("content-type", "text/html; charset=utf-16");//NOTICE UTF-16!!!

            PageContent result;
            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, "utf-16", "System.Text.UnicodeEncoding");
            Assert.IsFalse(result.Text.StartsWith(_contentString));//This should be random japanese text since we are using unicode
        }

        [TestMethod]
        public async Task GetContent_CharsetDefinedInBody_Shift_JIS_MetaDoubleQuotes_ReturnsCompletePageContentObject()
        {
            PageContent result;
            var httpResponseMessage = new HttpResponseMessage()
            {
                RequestMessage = new HttpRequestMessage(),
                Content = new FakeHttpContent(_contentString + "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=Shift_JIS\">")
            };

            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, JapaneseCharset);
            Assert.IsTrue(result.Text.StartsWith(_contentString));
        }


        [TestMethod]
        public async Task GetContent_CharsetDefinedInBody_Shift_JIS_MetaSingleQuotes_ReturnsCompletePageContentObject()
        {
            PageContent result;
            var httpResponseMessage = new HttpResponseMessage()
            {
                RequestMessage = new HttpRequestMessage(),
                Content = new FakeHttpContent(_contentString + "<meta http-equiv='Content-Type' content='text/html; charset=Shift_JIS'>")
            };

            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, JapaneseCharset);
            Assert.IsTrue(result.Text.StartsWith(_contentString));
        }

        [TestMethod]
        public async Task GetContent_CharsetDefinedInBody_Shift_JIS_MetaDoubleQuotes_WithClosingSlash_ReturnsCompletePageContentObject()
        {
            PageContent result;
            var httpResponseMessage = new HttpResponseMessage()
            {
                RequestMessage = new HttpRequestMessage(),
                Content = new FakeHttpContent(_contentString + "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=Shift_JIS\" />")
            };

            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, JapaneseCharset);
            Assert.IsTrue(result.Text.StartsWith(_contentString));
        }


        [TestMethod]
        public async Task GetContent_CharsetDefinedInBody_Shift_JIS_MetaSingleQuotes_WithClosingSlash_ReturnsCompletePageContentObject()
        {
            PageContent result;
            var httpResponseMessage = new HttpResponseMessage()
            {
                RequestMessage = new HttpRequestMessage(),
                Content = new FakeHttpContent(_contentString + "<meta http-equiv='Content-Type' content='text/html; charset=Shift_JIS' />")
            };

            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, JapaneseCharset);
            Assert.IsTrue(result.Text.StartsWith(_contentString));
        }


        [TestMethod]
        public async Task GetContent_CharsetDefinedInBody_Cp1251_MReturnsCompletePageContentObject()
        {
            PageContent result;
            var httpResponseMessage = new HttpResponseMessage()
            {
                RequestMessage = new HttpRequestMessage(),
                Content = new FakeHttpContent(_contentString + "<meta http-equiv=Content-Type content=\"text/html; charset=cp1251\">")
            };

            using (var response = httpResponseMessage)
            {
                result = await _uut.GetContentAsync(response);
            }

            MakeCommonAssertions(result, "windows-1251");
            Assert.IsTrue(result.Text.StartsWith(_contentString));
        }

        private void MakeCommonAssertions(PageContent result, string expectedCharset, string expectedEncodingString = JapaneseEncodingString)
        {
            Assert.IsNotNull(result.Bytes);
            Assert.AreNotEqual(0, result.Bytes.Length);
            Assert.AreEqual(expectedCharset, result.Charset);

            // Different between local and build server... Expected:<System.Text.UTF8Encoding+UTF8EncodingSealed> (local machine). Actual:<System.Text.DBCSCodePageEncoding> (Build server)
            //Assert.AreEqual(expectedEncodingString, result.Encoding.ToString());
            //Assert.AreEqual(Encoding.UTF8, encoding ?? Encoding.UTF8);
        }
    }
}
