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
    public abstract class HyperLinkParserTest
    {
        HyperLinkParser _unitUnderTest;
        Uri _uri = new Uri("http://a.com/");
        CrawledPage _crawledPage;

        protected abstract HyperLinkParser GetInstance();

        [SetUp]
        public void Setup()
        {
            _crawledPage = new CrawledPage(_uri){ HttpWebRequest = (HttpWebRequest)WebRequest.Create(_uri) };
            _unitUnderTest = GetInstance();
        }

        [Test]
        public void GetLinks_AnchorTags_ReturnsLinks()
        {
            _crawledPage.RawContent = "<a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>"; 

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);
            
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_AreaTags_ReturnsLinks()
        {
            _crawledPage.RawContent = "<area href=\"http://bbb.com\" /><area href=\"bbb/b.html\" />";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://bbb.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/bbb/b.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_NoLinks_NotReturned()
        {
            _crawledPage.RawContent = "<html></html>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_AnyScheme_Returned()
        {
            _crawledPage.RawContent = "<a href=\"mailto:aaa@gmail.com\" /><a href=\"tel:+123456789\" /><a href=\"callto:+123456789\" /><a href=\"ftp://user@yourdomainname.com/\" /><a href=\"file:///C:/Users/\" />";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(5, result.Count());
            Assert.AreEqual("mailto:aaa@gmail.com", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("tel:+123456789", result.ElementAt(1).AbsoluteUri);
            Assert.AreEqual("callto:+123456789", result.ElementAt(2).AbsoluteUri);
            Assert.AreEqual("ftp://user@yourdomainname.com/", result.ElementAt(3).AbsoluteUri);
            Assert.AreEqual("file:///C:/Users/", result.ElementAt(4).AbsoluteUri);
        }

        [Test]
        public void GetLinks_InvalidFormatUrl_NotReturned()
        {
            _crawledPage.RawContent = "<a href=\"http://////\" />";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_LinksInComments_NotReturned()
        {
            _crawledPage.RawContent = @"<html>
                    <head>
                        <!--
                            <a href='http://a1.com' />
                            <area href='http://a2.com' />
                        -->
                    </head>
                    <body>
                        <!--
                            <a href='http://b1.com' />
                            <area href='http://b2.com' />
                        -->
                    </body>
                    </html";

                IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

                Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_LinksInScript_NotReturned()
        {
            _crawledPage.RawContent = @"<html>
                    <head>
                        <script>
                            <a href='http://a1.com' />
                            <area href='http://a2.com' />
                        </script>
                    </head>
                    <body>
                        <script>
                            <a href='http://b1.com' />
                            <area href='http://b2.com' />
                        </script>
                    </body>
                    </html";

                IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

                Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_LinksInStyleTag_NotReturned()
        {
            _crawledPage.RawContent =  @"<html>
                    <head>
                        <style>
                            <a href='http://a1.com' />
                            <area href='http://a2.com' />
                        </style>
                    </head>
                    <body>
                        <style>
                            <a href='http://b1.com' />
                            <area href='http://b2.com' />
                        </style>
                    </body>
                    </html";

                IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

                Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_DuplicateLinks_ReturnsOnlyOne()
        {
            _crawledPage.RawContent = "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).AbsoluteUri);
        }

        [Test]
        public void GetLinks_NamedAnchors_Ignores()
        {
            _crawledPage.RawContent =  "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html#top\" ></a><a href=\"/aaa/a.html#bottom\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).AbsoluteUri);
        }

        [Test]
        public void GetLinks_EmptyHtml()
        {
            _crawledPage.RawContent =  "";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_WhiteSpaceHtml()
        {
            _crawledPage.RawContent = "         ";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_ValidBaseTagPresent_ReturnsRelativeLinksUsingBase()
        {
            _crawledPage.RawContent = "<base href=\"http://bbb.com\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://bbb.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_RelativeBaseTagPresent_ReturnsRelativeLinksPageUri()
        {
            _crawledPage.RawContent =  "<base href=\"/images\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_InvalidBaseTagPresent_ReturnsRelativeLinksPageUri()
        {
            _crawledPage.RawContent =  "<base href=\"http:http://http:\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetLinks_NullCrawledPage()
        {
            _unitUnderTest.GetLinks(null);
        }

        [Test]
        public void GetLinks_ResponseUriDiffFromRequestUri_UsesResponseUri()
        {
            _crawledPage.RawContent = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            //This sets the Address properties backing field which does not have a public set method
            ValueHelper.SetFieldValue(_crawledPage.HttpWebRequest, "_Uri", new Uri("http://zzz.com/"));

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://zzz.com/aaa/a.html", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://zzz.com/bbb/b.html", result.ElementAt(1).AbsoluteUri);
        }
    }
}
