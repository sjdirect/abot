using System;
using Abot.Core;
using Abot.Poco;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class PageRequesterTest
    {
        PageRequester _unitUnderTest;
        Uri _validUri = new Uri("http://localhost.fiddler:1111/");
        Uri _403ErrorUri = new Uri("http://localhost.fiddler:1111/HttpResponse/Status403");
        Uri _404ErrorUri = new Uri("http://localhost.fiddler:1111/HttpResponse/Status404");
        Uri _500ErrorUri = new Uri("http://localhost.fiddler:1111/HttpResponse/Status500");
        Uri _502ErrorUri = new Uri("http://www.lakkjfkasdfjhqlkfj.com");//non resolvable
        Uri _503ErrorUri = new Uri("http://localhost.fiddler:1111/HttpResponse/Status503");
        Uri _301To200Uri = new Uri("http://localhost.fiddler:1111/HttpResponse/Redirect/?redirectHttpStatus=301&destinationHttpStatus=200");
        Uri _301To404Uri = new Uri("http://localhost.fiddler:1111/HttpResponse/Redirect/?redirectHttpStatus=301&destinationHttpStatus=404");

        CrawlConfiguration _crawlConfig = new CrawlConfiguration { UserAgentString = "someuseragentstringhere" };

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new PageRequester(_crawlConfig);
        }

        [Test]
        public void Constructor_NullUserAgent()
        {
            Assert.Throws<ArgumentNullException>(() => new PageRequester(null));
        }

        [Test]
        public void MakeRequest_200_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_validUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(200, (int)result.HttpWebResponse.StatusCode);
            Assert.IsTrue(result.Content.Bytes.Length > 900 && result.Content.Bytes.Length < 1300);

            DateTime fiveSecsAgo = DateTime.Now.AddSeconds(-5);
            Assert.IsTrue(fiveSecsAgo < result.RequestStarted);
            Assert.IsTrue(fiveSecsAgo < result.RequestCompleted);
            Assert.IsNotNull(result.DownloadContentStarted);
            Assert.IsNotNull(result.DownloadContentCompleted);
            Assert.IsTrue(fiveSecsAgo < result.DownloadContentStarted);
            Assert.IsTrue(fiveSecsAgo < result.DownloadContentCompleted); 
        }

        [Test]
        public void MakeRequest_403_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_403ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(403, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (403) Forbidden.", result.WebException.Message);
            Assert.IsTrue(result.Content.Bytes.Length > 0);
        }

        [Test]
        public void MakeRequest_404_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_404ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(404, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (404) Not Found.", result.WebException.Message);
            Assert.IsTrue(result.Content.Bytes.Length > 0);
        }

        [Test]
        public void MakeRequest_500_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_500ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(500, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (500) Internal Server Error.", result.WebException.Message);
            Assert.IsTrue(result.Content.Bytes.Length > 0);
        }

        [Test]
        public void MakeRequest_503_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_503ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(503, (int)result.HttpWebResponse.StatusCode);
            Assert.IsTrue(result.Content.Bytes.Length > 0);

	        Assert.AreEqual("The remote server returned an error: (503) Server Unavailable.", result.WebException.Message);
        }

        [Test, Ignore("Cox intercepts 502 status and returns 200")]
        public void MakeHttpWebHeadRequest_NonResolvable_ReturnsNullResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_502ErrorUri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            //Assert.IsNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            //Assert.IsTrue(string.IsNullOrWhiteSpace(result.Content.Text));
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
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(200, (int)result.HttpWebResponse.StatusCode);
            Assert.IsTrue(result.Content.Bytes.Length > 0);

            DateTime fiveSecsAgo = DateTime.Now.AddSeconds(-5);
            Assert.IsTrue(fiveSecsAgo < result.RequestStarted);
            Assert.IsTrue(fiveSecsAgo < result.RequestCompleted);
            Assert.IsNotNull(result.DownloadContentStarted);
            Assert.IsNotNull(result.DownloadContentCompleted);
            Assert.IsTrue(fiveSecsAgo < result.DownloadContentStarted);
            Assert.IsTrue(fiveSecsAgo < result.DownloadContentCompleted); 
        }

        [Test]
        public void MakeRequest_AutoRedirect_301To404_ReturnsValidResponse()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_301To404Uri);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNotNull(result.WebException);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Content.Text));
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(404, (int)result.HttpWebResponse.StatusCode);
            Assert.AreEqual("The remote server returned an error: (404) Not Found.", result.WebException.Message);
            Assert.IsTrue(result.Content.Bytes.Length > 0);
        }

        [Test]
        public void MakeRequest_NullUri()
        {
            Assert.Throws<ArgumentNullException>(() => _unitUnderTest.MakeRequest(null));
        }

        [Test]
        public void MakeRequest_CrawlDecisionReturnsFalse_CrawlsPageButDoesNotDowloadContent()
        {
            CrawledPage result = _unitUnderTest.MakeRequest(_validUri, (x) => new CrawlDecision { Allow = false });

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.HttpWebRequest);
            Assert.IsNotNull(result.HttpWebResponse);
            Assert.IsNull(result.WebException);
            Assert.AreEqual("", result.Content.Text);
            Assert.IsNotNull(result.HtmlDocument);
            Assert.AreEqual(200, (int)result.HttpWebResponse.StatusCode);
            Assert.IsNull(result.Content.Bytes);

            DateTime fiveSecsAgo = DateTime.Now.AddSeconds(-5);
            Assert.IsTrue(fiveSecsAgo < result.RequestStarted);
            Assert.IsTrue(fiveSecsAgo < result.RequestCompleted);
            Assert.IsNull(result.DownloadContentStarted);
            Assert.IsNull(result.DownloadContentCompleted);
        }
    }
}
