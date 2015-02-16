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

        protected abstract HyperLinkParser GetInstance(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanUrlDelegate = null);

        [SetUp]
        public void Setup()
        {
            _crawledPage = new CrawledPage(_uri){ HttpWebRequest = (HttpWebRequest)WebRequest.Create(_uri) };
            _unitUnderTest = GetInstance(false, false);
        }

        [Test]
        public void GetLinks_AnchorTags_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>"; 

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);
            
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_AreaTags_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<area href=\"http://bbb.com\" /><area href=\"bbb/b.html\" />";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://bbb.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/bbb/b.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_AnchorTagsUpperCase_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<A HREF=\"http://aaa.com/\" ></A><A HREF=\"/aaa/a.html\" /></A>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_AreaTagsUpperCase_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<AREA HREF=\"http://bbb.com\" /><AREA HREF=\"bbb/b.html\" />";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://bbb.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/bbb/b.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_NoLinks_NotReturned()
        {
            _crawledPage.Content.Text = "<html></html>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_AnyScheme_Returned()
        {
            _crawledPage.Content.Text = "<a href=\"mailto:aaa@gmail.com\" /><a href=\"tel:+123456789\" /><a href=\"callto:+123456789\" /><a href=\"ftp://user@yourdomainname.com/\" /><a href=\"file:///C:/Users/\" />";

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
            _crawledPage.Content.Text = "<a href=\"http://////\" />";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_LinksInComments_NotReturned()
        {
            _crawledPage.Content.Text = @"<html>
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
            _crawledPage.Content.Text = @"<html>
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
            _crawledPage.Content.Text =  @"<html>
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
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).AbsoluteUri);
        }

        [Test]
        public void GetLinks_NamedAnchors_Ignores()
        {
            _crawledPage.Content.Text =  "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html#top\" ></a><a href=\"/aaa/a.html#bottom\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).AbsoluteUri);
        }

        [Test]
        public void GetLinks_EmptyHtml()
        {
            _crawledPage.Content.Text =  "";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_WhiteSpaceHtml()
        {
            _crawledPage.Content.Text = "         ";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_ValidBaseTagPresent_ReturnsRelativeLinksUsingBase()
        {
            _crawledPage.Content.Text = "<base href=\"http://bbb.com\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://bbb.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_RelativeBaseTagPresent_ReturnsRelativeLinksPageUri()
        {
            _crawledPage.Content.Text =  "<base href=\"/images\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_InvalidBaseTagPresent_ReturnsRelativeLinksPageUri()
        {
            _crawledPage.Content.Text =  "<base href=\"http:http://http:\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

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
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            //This sets the Address properties backing field which does not have a public set method
            ValueHelper.SetFieldValue(_crawledPage.HttpWebRequest, "_Uri", new Uri("http://zzz.com/"));

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://zzz.com/aaa/a.html", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://zzz.com/bbb/b.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test]
        public void GetLinks_HtmlEncodedHref_UrlDecodes()
        {
            _crawledPage.Content.Text = "<a href=\"http://a.com/search?rls=en&amp;q=stack+overflow\" ></a>";

            //This sets the Address properties backing field 
            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/search?rls=en&q=stack+overflow", result.ElementAt(0).AbsoluteUri);
        }


        [Test]
        public void GetLinks_MetaNoIndexNoFollowNotSet_ReturnsLinks()
        {
            _unitUnderTest = GetInstance(false, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"noindex, nofollow\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void GetLinks_MetaNoIndexNoFollow_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"noindex, nofollow\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_MetaNoIndexNoFollowUpperCase_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false);
            _crawledPage.Content.Text = "<META NAME=\"ROBOTS\" CONTENT=\"NOINDEX, NOFOLLOW\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_MetaNoIndexNoFollowUsingNone_ReturnsLinks()
        {
            _unitUnderTest = GetInstance(true, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"none\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_MetaNoIndexNoFollowUsingNoneUpperCase_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false);
            _crawledPage.Content.Text = "<META NAME=\"ROBOTS\" CONTENT=\"NONE\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_NoFollow_NotReturned()
        {
            _unitUnderTest = GetInstance(true, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"nofollow\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_MetaNoIndex_ReturnsLinks()
        {
            _unitUnderTest = GetInstance(true, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"noindex\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }


        [Test]
        public void GetLinks_RelNoFollow_NotReturned()
        {
            _unitUnderTest = GetInstance(false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" rel=\"nofollow\"></a><a href=\"/bbb/b.html\" rel=\"nofollow\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_RelNoFollowUpperCase_NotReturned()
        {
            _unitUnderTest = GetInstance(false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" REL=\"NOFOLLOW\"></a><a href=\"/bbb/b.html\" REL=\"NOFOLLOW\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetLinks_CleanUrlDelegateSet_ReturnsCleanLinks()
        {
            _unitUnderTest = GetInstance(false, false, (x) => x.Replace("a", "x").Replace("b", "y"));
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://a.com/xxx/x.html", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/yyy/y.html", result.ElementAt(1).AbsoluteUri);
        }

        [Test] //https://github.com/sjdirect/abot/issues/15
        public void GetLinks_ColonInUrl_DoesNotThrowException()
        {
            _crawledPage.Content.Text = "<a href=\"http://www.gamespot.com/pc/rpg/numen/index.html?om_act=convert&om_clk=tabs&tag=tabs;summary\" ></a>";
            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://www.gamespot.com/pc/rpg/numen/index.html?om_act=convert&om_clk=tabs&tag=tabs;summary", result.ElementAt(0).AbsoluteUri);
        }

        [Test]
        public void GetLinks_LinkRelConical_ReturnsLink()
        {
            _crawledPage.Content.Text = "<html><head><link rel=\"canonical\" href=\"http://a.com/page1\" /></head><body><a href=\"http://a.com/page2\"></a></body></html>";
            IEnumerable<Uri> result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://a.com/page2", result.ElementAt(0).AbsoluteUri);
            Assert.AreEqual("http://a.com/page1", result.ElementAt(1).AbsoluteUri);
        }
    }
}
