using Abot.Core;
using Abot.Poco;
using Moq;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class RobotsDotTextFinderTest
    {
        RobotsDotTextFinder _uut;
        Mock<IPageRequester> _fakePageRequester;
        CrawledPage _goodPageResult;
        CrawledPage _badPageResult;

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            PageRequester pageRequster = new PageRequester(new CrawlConfiguration { UserAgentString = "aaa" });
            _goodPageResult = pageRequster.MakeRequest(new Uri("http://localhost.fiddler:1111/"));
            _badPageResult = pageRequster.MakeRequest(new Uri("http://localhost.fiddler:1111/HttpResponse/Status404"));
        }

        [SetUp]
        public void SetUp()
        {
            _fakePageRequester = new Mock<IPageRequester>();
            _uut = new RobotsDotTextFinder(_fakePageRequester.Object);
        }

        [Test]
        public void Constructor_PageRequesterIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new RobotsDotTextFinder(null));
        }

        [Test]
        public void Find_NullUrl()
        {
            Assert.Throws<ArgumentNullException>(() => _uut.Find(null));
        }

        [Test]
        public void Find_RobotsExists_UriIsDomain_ReturnsRobotsDotText()
        {
            Uri _rootUri = new Uri("http://a.com/");
            Uri expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            IRobotsDotText result = _uut.Find(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [Test]
        public void Find_RobotsExists_UriIsSubDomain_ReturnsRobotsDotText()
        {
            Uri _rootUri = new Uri("http://aaa.a.com/");
            Uri expectedRobotsUri = new Uri("http://aaa.a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            IRobotsDotText result = _uut.Find(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [Test]
        public void Find_RobotsExists_UriIsNotRootDomain_ReturnsRobotsDotText()
        {
            Uri _rootUri = new Uri("http://a.com/a/b/b.html");
            Uri expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            IRobotsDotText result = _uut.Find(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [Test]
        public void Find_RobotsExists_UriIsRootDomainNoSlash_ReturnsRobotsDotText()
        {
            Uri _rootUri = new Uri("http://a.com");
            Uri expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            IRobotsDotText result = _uut.Find(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [Test]
        public void Find_RobotsExists_RootAndExpectedAreSame_ReturnsRobotsDotText()
        {
            Uri _rootUri = new Uri("http://a.com/robots.txt");
            Uri expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            IRobotsDotText result = _uut.Find(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [Test]
        public void Find_RobotsDoesNotExists_ReturnsNull()
        {
            Uri _rootUri = new Uri("http://a.com/");
            Uri expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_badPageResult);

            IRobotsDotText result = _uut.Find(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequest(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNull(result);
        }
    }
}
