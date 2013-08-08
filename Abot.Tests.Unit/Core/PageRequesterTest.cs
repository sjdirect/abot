using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.Reflection;


namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class PageRequesterTest
    {
        PageRequester _unitUnderTest;
        Uri _validUri = new Uri("http://localhost:1111/");
        Uri _403ErrorUri = new Uri("http://localhost:1111/HttpResponse/Status403");
        Uri _404ErrorUri = new Uri("http://localhost:1111/HttpResponse/Status404");
        Uri _500ErrorUri = new Uri("http://localhost:1111/HttpResponse/Status500");
        Uri _502ErrorUri = new Uri("http://www.lakkjfkasdfjhqlkfj.com");//non resolvable
        Uri _503ErrorUri = new Uri("http://localhost:1111/HttpResponse/Status503");
        Uri _301To200Uri = new Uri("http://localhost:1111/HttpResponse/Redirect/?redirectHttpStatus=301&destinationHttpStatus=200");
        Uri _301To404Uri = new Uri("http://localhost:1111/HttpResponse/Redirect/?redirectHttpStatus=301&destinationHttpStatus=404");

        CrawlConfiguration _crawlConfig = new CrawlConfiguration { UserAgentString = "someuseragentstringhere" };

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new PageRequester(_crawlConfig);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullUserAgent()
        {
            new PageRequester(null);
        }

        [Test]
        public void Constructor_SetsUserAgent()
        {
            Assert.AreEqual(_crawlConfig.UserAgentString, new PageRequesterWrapper(_crawlConfig).UserAgentWrapper);
        }

        [Test]
        public void Constructor_SetsUserAgentWithAssemblyVersion()
        {
            _crawlConfig.UserAgentString = "ha @ABOTASSEMBLYVERSION@ ha";
            Assert.AreEqual(string.Format("ha {0} ha", Assembly.GetAssembly(this.GetType()).GetName().Version.ToString()), new PageRequesterWrapper(_crawlConfig).UserAgentWrapper);
        }

        [Test]
        public void MakeRequest_200_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_validUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(200, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual(938, result.PageSizeInBytes);
        }

        [Test]
        public void MakeRequest_403_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_403ErrorUri);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(403, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (403) Forbidden.", result.WebException.Message);
            Assert.IsTrue(result.PageSizeInBytes > 0);
        }

        [Test]
        public void MakeRequest_404_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_404ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(404, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (404) Not Found.", result.WebException.Message);
            Assert.IsTrue(result.PageSizeInBytes > 0);
        }

        [Test]
        public void MakeRequest_500_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_500ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(500, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (500) Internal Server Error.", result.WebException.Message);
            Assert.IsTrue(result.PageSizeInBytes > 0);
        }

        [Test]
        public void MakeRequest_503_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_503ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(503, (int)result.HttpWebResponse.StatusCode);
            Assert.IsTrue(result.PageSizeInBytes > 0);

	        Assert.AreEqual("The remote server returned an error: (503) Server Unavailable.", result.WebException.Message);
        }

        [Test, Ignore]//Cox intercepts 502 status and returns 200
        public void MakeHttpWebHeadRequest_NonResolvable_ReturnsNullResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_502ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            //Assert.IsNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            //Assert.IsTrue(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsTrue(result.WebException.Message.StartsWith("The remote name could not be resolved: ") || result.WebException.Message.StartsWith("The remote server returned an error: (502) Bad Gateway."));
        }

        [Test]
        public void MakeRequest_AutoRedirect_301To200_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_301To200Uri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(200, (int)result.HttpWebResponse.StatusCode);
            Assert.IsTrue(result.PageSizeInBytes > 0);
        }

        [Test]
        public void MakeRequest_AutoRedirect_301To404_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_301To404Uri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RawContent));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(404, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (404) Not Found.", result.WebException.Message);
            Assert.IsTrue(result.PageSizeInBytes > 0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MakeRequest_NullUri()
        {
            _unitUnderTest.MakeRequest(null);
        }

        [Test]
        public void MakeRequest_CrawlDecisionReturnsFalse_CrawlsPageButDoesNotDowloadContent()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_validUri, (x) => new CrawlDecision { Allow = false });

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNull(result.WebException);
            Assert.AreEqual("", result.RawContent);
            Assert.IsNotNull(result.HtmlDocument);
            Assert.IsNotNull(result.CsQueryDocument);
            Assert.AreEqual(200, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual(0, result.PageSizeInBytes);
        }
    }

    public class PageRequesterWrapper : PageRequester
    {
        public string UserAgentWrapper { get{return base._userAgentString;} private set{} }
        public PageRequesterWrapper(CrawlConfiguration config)
            : base(config)
        {
        }

    }
}
