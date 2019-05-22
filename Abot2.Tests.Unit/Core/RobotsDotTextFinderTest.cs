using Abot2.Core;
using Abot2.Poco;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class RobotsDotTextFinderTest
    {
        RobotsDotTextFinder _uut;
        Mock<IPageRequester> _fakePageRequester;
        readonly Task<CrawledPage> _goodPageResult = Task.FromResult(new CrawledPage(new Uri("http://google.com")){HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)});
        readonly Task<CrawledPage> _badPageResult = Task.FromResult(new CrawledPage(new Uri("http://askjfdhakshdfkashdfkashdfkjah.com")) { HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound) });

        [TestInitialize]
        public void SetUp()
        {
            _fakePageRequester = new Mock<IPageRequester>();
            _uut = new RobotsDotTextFinder(_fakePageRequester.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_PageRequesterIsNull() => new RobotsDotTextFinder(null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Find_NullUrl() => await _uut.FindAsync(null);

        [TestMethod]
        public async Task Find_RobotsExists_UriIsDomain_ReturnsRobotsDotText()
        {
            var _rootUri = new Uri("http://a.com/");
            var expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            var result = await _uut.FindAsync(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Find_RobotsExists_UriIsSubDomain_ReturnsRobotsDotText()
        {
            var _rootUri = new Uri("http://aaa.a.com/");
            var expectedRobotsUri = new Uri("http://aaa.a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            var result = await _uut.FindAsync(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Find_RobotsExists_UriIsNotRootDomain_ReturnsRobotsDotText()
        {
            var _rootUri = new Uri("http://a.com/a/b/b.html");
            var expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            var result = await _uut.FindAsync(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Find_RobotsExists_UriIsRootDomainNoSlash_ReturnsRobotsDotText()
        {
            var _rootUri = new Uri("http://a.com");
            var expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            var result = await _uut.FindAsync(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Find_RobotsExists_RootAndExpectedAreSame_ReturnsRobotsDotText()
        {
            var _rootUri = new Uri("http://a.com/robots.txt");
            var expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_goodPageResult);

            var result = await _uut.FindAsync(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Find_RobotsDoesNotExists_ReturnsNull()
        {
            var _rootUri = new Uri("http://a.com/");
            var expectedRobotsUri = new Uri("http://a.com/robots.txt");
            _fakePageRequester.Setup(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri))).Returns(_badPageResult);

            var result = await _uut.FindAsync(_rootUri);

            _fakePageRequester.Verify(f => f.MakeRequestAsync(It.Is<Uri>(u => u == expectedRobotsUri)), Times.Exactly(1));
            Assert.IsNull(result);
        }
    }
}
