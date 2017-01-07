using Abot.SiteSimulator.Controllers;
using NUnit.Framework;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Test.Controllers
{
    [TestFixture]
    public class ThrottlingControllerTest
    {
        ThrottlingController _unitUnderTest;

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new ThrottlingController();
            _unitUnderTest.ResetLastServcedDate();
        }

        [Test]
        public void Index_ReturnsDefaultView()
        {
            ViewResult result = _unitUnderTest.Index() as ViewResult;
            Assert.AreEqual("Index", result.ViewName);
        }

        [Test]
        public void Handle1RequestEveryXSeconds_AboveXSecondsSinceLastRequest_Generates200StatusPage()
        {
            ViewResult result = _unitUnderTest.Handle1RequestEveryXSeconds(1) as ViewResult;
            Assert.AreEqual("BlankPage", result.ViewName);
        }

        [Test]
        public void Handle1RequestEveryXSeconds_2ndRequestWithin1Second()
        {
            _unitUnderTest.Handle1RequestEveryXSeconds(1);
            Assert.Throws<System.Web.HttpException>(() =>_unitUnderTest.Handle1RequestEveryXSeconds(1));
        }

        [Test]
        public void Handle1RequestEveryXSeconds_2ndRequestAfter1Second_DoesNotThrowException()
        {
            _unitUnderTest.Handle1RequestEveryXSeconds(1);
            System.Threading.Thread.Sleep(1001);
            ViewResult result = _unitUnderTest.Handle1RequestEveryXSeconds(1) as ViewResult;

            Assert.AreEqual("BlankPage", result.ViewName);
        }

        [Test]
        public void RssetLastService_DoesNotThrowException()
        {
            _unitUnderTest.ResetLastServcedDate();
        }
    }
}
