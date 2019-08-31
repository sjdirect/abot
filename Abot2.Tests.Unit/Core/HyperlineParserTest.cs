using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public abstract class HyperLinkParserTest
    {
        HyperLinkParser _unitUnderTest;
        Uri _uri = new Uri("http://a.com/");
        CrawledPage _crawledPage;

        protected abstract HyperLinkParser GetInstance(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanUrlDelegate, bool isRespectUrlNamedAnchorOrHashbangEnabled, bool isRespectHttpXRobotsTagHeaderNoFollow);

        [TestInitialize]
        public void Init()
        {
            _crawledPage = new CrawledPage(_uri);
            
            _crawledPage.ParentUri = _uri;
            _crawledPage.HttpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _uri);
            _crawledPage.HttpResponseMessage = new HttpResponseMessage();

            _unitUnderTest = GetInstance(false, false, null, false, false);
        }

        [TestMethod]
        public void GetLinks_AnchorTags_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_AreaTags_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<area href=\"http://bbb.com\" /><area href=\"bbb/b.html\" />";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://bbb.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/bbb/b.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_AnchorTagsUpperCase_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<A HREF=\"http://aaa.com/\" ></A><A HREF=\"/aaa/a.html\" /></A>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_AreaTagsUpperCase_ReturnsLinks()
        {
            _crawledPage.Content.Text = "<AREA HREF=\"http://bbb.com\" /><AREA HREF=\"bbb/b.html\" />";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://bbb.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/bbb/b.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_NoLinks_NotReturned()
        {
            _crawledPage.Content.Text = "<html></html>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_AnyScheme_Returned()
        {
            _crawledPage.Content.Text = "<a href=\"mailto:aaa@gmail.com\" /><a href=\"tel:+123456789\" /><a href=\"callto:+123456789\" /><a href=\"ftp://user@yourdomainname.com/\" /><a href=\"file:///C:/Users/\" />";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(5, result.Count());
            Assert.AreEqual("mailto:aaa@gmail.com", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("tel:+123456789", result.ElementAt(1).HrefValue.AbsoluteUri);
            Assert.AreEqual("callto:+123456789", result.ElementAt(2).HrefValue.AbsoluteUri);
            Assert.AreEqual("ftp://user@yourdomainname.com/", result.ElementAt(3).HrefValue.AbsoluteUri);
            Assert.AreEqual("file:///C:/Users/", result.ElementAt(4).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_InvalidFormatUrl_NotReturned()
        {
            _crawledPage.Content.Text = "<a href=\"http://////\" />";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
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

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
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

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_LinksInStyleTag_NotReturned()
        {
            _crawledPage.Content.Text = @"<html>
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

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_DuplicateLinks_ReturnsOnlyOne()
        {
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_NamedAnchorsOrHashbangs_Ignores()
        {
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html#top\" ></a><a href=\"/aaa/a.html#bottom\" /></a><a href=\"/aaa/a.html/#someaction/someid\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html/", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_NamedAnchorsOrHashbangs_Enabled_ReturnsLinks()
        {
            _unitUnderTest = GetInstance(false, false, null, true, false);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/aaa/a.html#top\" ></a><a href=\"/aaa/a.html#bottom\" /></a><a href=\"/aaa/a.html/#someaction/someid\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(4, result.Count());
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html#top", result.ElementAt(1).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html#bottom", result.ElementAt(2).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html/#someaction/someid", result.ElementAt(3).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_EmptyHtml()
        {
            _crawledPage.Content.Text = "";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_WhiteSpaceHtml()
        {
            _crawledPage.Content.Text = "         ";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_ValidBaseTagPresent_ReturnsRelativeLinksUsingBase()
        {
            _crawledPage.Content.Text = "<base href=\"http://bbb.com\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://bbb.com/aaa/a.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_RelativeBaseTagPresent_ReturnsRelativeLinksPageUri()
        {
            _crawledPage.Content.Text = "<base href=\"/images\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_InvalidBaseTagPresent_ReturnsRelativeLinksPageUri()
        {
            _crawledPage.Content.Text = "<base href=\"http:http://http:\"><a href=\"http://aaa.com/\" ></a><a href=\"/aaa/a.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://aaa.com/", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/aaa/a.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_BaseTagNoScheme_ParentPageHttp_AddsParentPageScheme()
        {
            _crawledPage.Uri = new Uri("http://aaa.com/");//http
            _crawledPage.Content.Text = "<base href=\"//aaa.com\"><a href=\"/aaa/a.html\" ></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://aaa.com/aaa/a.html", result.ElementAt(0).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_BaseTagNoScheme_ParentPageHttps_AddsParentPageScheme()
        {
            _crawledPage.Uri = new Uri("https://aaa.com/");//https
            _crawledPage.Content.Text = "<base href=\"//aaa.com\"><a href=\"/aaa/a.html\" ></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("https://aaa.com/aaa/a.html", result.ElementAt(0).HrefValue.AbsoluteUri);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetLinks_NullCrawledPage_ThrowsException()
        {
            _unitUnderTest.GetLinks(null);
        }

        [TestMethod, Ignore("Not sure how to handle this just yet")]
        public void GetLinks_ResponseUriDiffFromRequestUri_UsesResponseUri()
        {
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";
            
            //This sets the Address properties backing field which does not have a public set method
            //ValueHelper.SetFieldValue(_crawledPage.HttpWebRequest, "_Uri", new Uri("http://zzz.com/"));
            
            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://zzz.com/aaa/a.html", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://zzz.com/bbb/b.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_HtmlEncodedHref_UrlDecodes()
        {
            _crawledPage.Content.Text = "<a href=\"http://a.com/search?rls=en&amp;q=stack+overflow\" ></a>";

            //This sets the Address properties backing field 
            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://a.com/search?rls=en&q=stack+overflow", result.ElementAt(0).HrefValue.AbsoluteUri);
        }


        [TestMethod]
        public void GetLinks_MetaNoIndexNoFollowNotSet_ReturnsLinks()
        {
            _unitUnderTest = GetInstance(false, false, null, false, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"noindex, nofollow\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public void GetLinks_MetaNoIndexNoFollow_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false, null, false, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"noindex, nofollow\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_MetaNoIndexNoFollowUpperCase_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false, null, false, false);
            _crawledPage.Content.Text = "<META NAME=\"ROBOTS\" CONTENT=\"NOINDEX, NOFOLLOW\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_MetaNoIndexNoFollowUsingNone_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false, null, false, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"none\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_MetaNoIndexNoFollowUsingNoneUpperCase_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false, null, false, false);
            _crawledPage.Content.Text = "<META NAME=\"ROBOTS\" CONTENT=\"NONE\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_MetaNoFollow_ReturnsEmptyList()
        {
            _unitUnderTest = GetInstance(true, false, null, false, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"nofollow\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_MetaNoIndex_ReturnsLinks()
        {
            _unitUnderTest = GetInstance(true, false, null, false, false);
            _crawledPage.Content.Text = "<meta name=\"robots\" content=\"noindex\" /><a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }


        [TestMethod]
        public void GetLinks_HttpXRobotsTagHeaderNoIndexNoFollow_ReturnsEmptyList()
        {
            _crawledPage.HttpResponseMessage.Headers.Add("X-Robots-Tag", new List<string>(){"noindex, nofollow"});
            _unitUnderTest = GetInstance(false, false, null, false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_HttpXRobotsTagHeaderNoIndexNoFollowUpperCase_ReturnsEmptyList()
        {
            _crawledPage.HttpResponseMessage.Headers.Add("X-Robots-Tag", new List<string>() { "NOINDEX, NOFOLLOW" });
            _unitUnderTest = GetInstance(false, false, null, false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_HttpXRobotsTagHeaderNoIndexNoFollowUsingNone_ReturnsEmptyList()
        {
            _crawledPage.HttpResponseMessage.Headers.Add("X-Robots-Tag", new List<string>(){"none"});
            _unitUnderTest = GetInstance(false, false, null, false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_HttpXRobotsTagHeaderNoIndexNoFollowUsingNoneUpperCase_ReturnsEmptyList()
        {
            _crawledPage.HttpResponseMessage.Headers.Add("X-Robots-Tag", new List<string>() { "NONE" });
            _unitUnderTest = GetInstance(false, false, null, false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_HttpXRobotsNoFollow_ReturnsEmptyList()
        {
            _crawledPage.HttpResponseMessage.Headers.Add("X-Robots-Tag", new List<string>(){"nofollow"} );
            _unitUnderTest = GetInstance(false, false, null, false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_HttpXRobotsTagHeaderNoIndex_ReturnsLinks()
        {
            _crawledPage.HttpResponseMessage.Headers.Add("X-Robots-Tag", new List<string>() { "noindex" });
            _unitUnderTest = GetInstance(false, false, null, false, true);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }


        [TestMethod]
        public void GetLinks_RelNoFollow_NotReturned()
        {
            _unitUnderTest = GetInstance(false, true, null, false, false);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" rel=\"nofollow\"></a><a href=\"/bbb/b.html\" rel=\"nofollow\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_RelNoFollowUpperCase_NotReturned()
        {
            _unitUnderTest = GetInstance(false, true, null, false, false);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" REL=\"NOFOLLOW\"></a><a href=\"/bbb/b.html\" REL=\"NOFOLLOW\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetLinks_CleanUrlDelegateSet_ReturnsCleanLinks()
        {
            _unitUnderTest = GetInstance(false, false, (x) => x.Replace("a", "x").Replace("b", "y"), false, false);
            _crawledPage.Content.Text = "<a href=\"/aaa/a.html\" ></a><a href=\"/bbb/b.html\" /></a>";

            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://x.com/xxx/x.html", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://x.com/yyy/y.html", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

        [TestMethod] //https://github.com/sjdirect/abot/issues/15
        public void GetLinks_ColonInUrl_DoesNotThrowException()
        {
            _crawledPage.Content.Text = "<a href=\"http://www.gamespot.com/pc/rpg/numen/index.html?om_act=convert&om_clk=tabs&tag=tabs;summary\" ></a>";
            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("http://www.gamespot.com/pc/rpg/numen/index.html?om_act=convert&om_clk=tabs&tag=tabs;summary", result.ElementAt(0).HrefValue.AbsoluteUri);
        }

        [TestMethod]
        public void GetLinks_LinkRelConical_ReturnsLink()
        {
            _crawledPage.Content.Text = "<html><head><link rel=\"canonical\" href=\"http://a.com/page1\" /></head><body><a href=\"http://a.com/page2\"></a></body></html>";
            var result = _unitUnderTest.GetLinks(_crawledPage);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("http://a.com/page2", result.ElementAt(0).HrefValue.AbsoluteUri);
            Assert.AreEqual("http://a.com/page1", result.ElementAt(1).HrefValue.AbsoluteUri);
        }

    }
}
