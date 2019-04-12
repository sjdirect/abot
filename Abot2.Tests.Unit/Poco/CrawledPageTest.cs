using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class CrawledPageTest
    {
        [TestMethod]
        public void Constructor_ValidUri_CreatesInstance()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"));
            Assert.AreEqual(null, unitUnderTest.HttpRequestMessage);
            Assert.AreEqual(null, unitUnderTest.HttpResponseMessage);
            Assert.AreEqual(false, unitUnderTest.IsRetry);
            Assert.AreEqual(null, unitUnderTest.ParentUri);
            Assert.IsNotNull(unitUnderTest.Content);
            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.AreEqual("http://a.com/", unitUnderTest.Uri.AbsoluteUri);
            Assert.AreEqual(null, unitUnderTest.HttpRequestException);
            Assert.AreEqual(null, unitUnderTest.ParsedLinks);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_InvalidUri_ThrowsException()
        {
            new CrawledPage(null);
        }

        [TestMethod]
        public void AngleSharpHtmlDocument_ContentIsValid_AngleSharpHtmlDocumentIsNotNull()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent 
                { 
                    Text = "hi there" 
                } 
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.AreEqual("hi there", unitUnderTest.AngleSharpHtmlDocument.Body.TextContent);
        }

        [TestMethod]
        public void AngleSharpHtmlDocument_RawContentIsNull_AngleSharpHtmlDocumentIsNotNull()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent 
                { 
                    Text = null
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.AreEqual("", unitUnderTest.AngleSharpHtmlDocument.Body.TextContent);
        }

        [TestMethod]
        public void AngleSharpHtmlDocument_ToManyNestedTagsInSource1_DoesNotCauseStackOverflowException()
        {
            //FYI this test will not fail, it will just throw an uncatchable stackoverflowexception that will kill the process that runs this test
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow1.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.IsTrue(unitUnderTest.AngleSharpHtmlDocument.Body.TextContent.Length > 0);
        }

        [TestMethod]
        public void AngleSharpHtmlDocument_ToManyNestedTagsInSource2_DoesNotCauseStackOverflowException()
        {
            //FYI this test will not fail, it will just throw an uncatchable stackoverflowexception that will kill the process that runs this test
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow2.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.IsTrue(unitUnderTest.AngleSharpHtmlDocument.Body.TextContent.Length > 0);
        }


        [TestMethod]
        public void AngleSharpDocument_RawContentIsNull_AngleSharpDocumentIsNotNull()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = null
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
        }

        [TestMethod]
        public void AngleSharpDocument_ToManyNestedTagsInSource1_DoesNotCauseStackOverflowException()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow1.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.IsTrue(unitUnderTest.AngleSharpHtmlDocument.ToString().Length > 1);
        }

        [TestMethod]
        public void AngleSharpDocument_ToManyNestedTagsInSource2_DoesNotCauseStackOverflowException()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow2.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.IsTrue(unitUnderTest.AngleSharpHtmlDocument.ToString().Length > 1);
        }

        [TestMethod]
        public void AngleSharp_EncodingChangedTwice_IsLoaded()
        {
            var unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = @"<div>hehe</div><meta http-equiv=""Content-Type"" content=""text/html; charset=iso-8859-1""><meta http-equiv=""content-type"" content=""text/html; charset=utf-8"" /><div>hi</div>"
                }
            };

            Assert.IsNotNull(unitUnderTest.AngleSharpHtmlDocument);
            Assert.AreEqual(7, unitUnderTest.AngleSharpHtmlDocument.All.Length);
        }


        [TestMethod]
        public void ToString_HttpResponseDoesNotExists_MessageHasUri()
        {
            Assert.AreEqual("http://localhost.fiddler:1111/", new CrawledPage(new Uri("http://localhost.fiddler:1111/")).ToString());
        }

        [TestMethod]
        public async Task ToString_HttpResponseExists_MessageHasUriAndStatus()
        {
            var result =
                await new PageRequester(new CrawlConfiguration(), new WebContentExtractor()).MakeRequestAsync(
                    new Uri("http://google.com/"));

            Assert.AreEqual("http://google.com/[200]", result.ToString());
        }

        [TestMethod]
        public void Elapsed_ReturnsDiffInMilli()
        {
            var uut = new CrawledPage(new Uri("http://a.com"))
            {
                RequestStarted = DateTime.Now.AddSeconds(-5),
                RequestCompleted = DateTime.Now
            };

            Assert.IsTrue(uut.Elapsed >= 5000, "Expected >= 5000 but was " + uut.Elapsed);
        }

        private string GetFileContent(string fileName)
        {
            //var testFile = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, fileName));
            var testFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName));
            
            if (!File.Exists(testFile))
                throw new ApplicationException("Cannot find file " + fileName);

            return File.ReadAllText(testFile);
        }
    }
}
