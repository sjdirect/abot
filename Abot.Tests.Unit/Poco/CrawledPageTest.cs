using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.IO;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class CrawledPageTest
    {
        [Test]
        public void Constructor_ValidUri_CreatesInstance()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/"));
            Assert.AreEqual(null, unitUnderTest.HttpWebRequest);
            Assert.AreEqual(null, unitUnderTest.HttpWebResponse);
            Assert.AreEqual(false, unitUnderTest.IsRetry);
            Assert.AreEqual(null, unitUnderTest.ParentUri);
            Assert.IsNotNull(unitUnderTest.Content);
            Assert.IsNotNull(unitUnderTest.HtmlDocument);
            Assert.IsNotNull(unitUnderTest.CsQueryDocument);
            Assert.AreEqual("http://a.com/", unitUnderTest.Uri.AbsoluteUri);
            Assert.AreEqual(null, unitUnderTest.WebException);
            Assert.AreEqual(null, unitUnderTest.ParsedLinks);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_InvalidUri()
        {
            new CrawledPage(null);
        }

        [Test]
        public void HtmlDocument_ContentIsValid_HtmlDocumentIsNotNull()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent 
                { 
                    Text = "hi there" 
                } 
            };

            Assert.IsNotNull(unitUnderTest.HtmlDocument);
            Assert.AreEqual("hi there", unitUnderTest.HtmlDocument.DocumentNode.InnerText);
        }

        [Test]
        public void HtmlDocument_RawContentIsNull_HtmlDocumentIsNotNull()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent 
                { 
                    Text = null
                }
            };

            Assert.IsNotNull(unitUnderTest.HtmlDocument);
            Assert.AreEqual("", unitUnderTest.HtmlDocument.DocumentNode.InnerText);
        }

        [Test]
        public void HtmlDocument_ToManyNestedTagsInSource1_DoesNotCauseStackOverflowException()
        {
            //FYI this test will not fail, it will just throw an uncatchable stackoverflowexception that will kill the process that runs this test
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/"))
            {
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow1.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.HtmlDocument);
            Assert.AreEqual("", unitUnderTest.HtmlDocument.DocumentNode.InnerText);
        }

        [Test]
        public void HtmlDocument_ToManyNestedTagsInSource2_DoesNotCauseStackOverflowException()
        {
            //FYI this test will not fail, it will just throw an uncatchable stackoverflowexception that will kill the process that runs this test
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow2.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.HtmlDocument);
            Assert.AreEqual("", unitUnderTest.HtmlDocument.DocumentNode.InnerText);
        }

        [Test]
        public void CsQueryDocument_RawContentIsNull_CsQueryDocumentIsNotNull()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            {
                Content = new PageContent
                {
                    Text = null
                }
            };

            Assert.IsNotNull(unitUnderTest.CsQueryDocument);
        }

        [Test]
        public void CsQueryDocument_ToManyNestedTagsInSource1_DoesNotCauseStackOverflowException()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow1.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.CsQueryDocument);
            Assert.IsTrue(unitUnderTest.CsQueryDocument.ToString().Length > 1);
        }

        [Test, Ignore("This test passes but takes 28 seconds to run")]
        public void CsQueryDocument_ToManyNestedTagsInSource2_DoesNotCauseStackOverflowException()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent
                {
                    Text = GetFileContent("HtmlAgilityPackStackOverflow2.html")
                }
            };

            Assert.IsNotNull(unitUnderTest.CsQueryDocument);
            Assert.IsTrue(unitUnderTest.CsQueryDocument.ToString().Length > 1);
        }

        [Test]
        public void CsQuery_EncodingChangedTwice_IsLoaded()
        {
            CrawledPage unitUnderTest = new CrawledPage(new Uri("http://a.com/")) 
            { 
                Content = new PageContent
                {
                    Text = @"<div>hehe</div><meta http-equiv=""Content-Type"" content=""text/html; charset=iso-8859-1""><meta http-equiv=""content-type"" content=""text/html; charset=utf-8"" /><div>hi</div>"
                } 
            };

            Assert.IsNotNull(unitUnderTest.CsQueryDocument);
            Assert.AreEqual(4, unitUnderTest.CsQueryDocument.Length);
        }

        [Test]
        public void ToString_HttpResponseDoesNotExists_MessageHasUri()
        {
            Assert.AreEqual("http://localhost.fiddler:1111/", new CrawledPage(new Uri("http://localhost.fiddler:1111/")).ToString());
        }

        [Test]
        public void ToString_HttpResponseExists_MessageHasUriAndStatus()
        {
            Assert.AreEqual("http://localhost.fiddler:1111/[200]", new PageRequester(new CrawlConfiguration{ UserAgentString = "aaa" }).MakeRequest(new Uri("http://localhost.fiddler:1111/")).ToString());
        }

        private string GetFileContent(string fileName)
        {
            if (!File.Exists(fileName))
                throw new ApplicationException("Cannot find file " + fileName);

            return File.ReadAllText(fileName);
        }
    }
}
