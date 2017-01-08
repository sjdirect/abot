using Abot.SiteSimulator.Controllers;
using NUnit.Framework;
using System;
using System.Web;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Test.Unit.Controllers
{
    [TestFixture]
    public class HttpResponseControllerTest
    {
        HttpResponseController _unitUnderTest;

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new HttpResponseController();
        }

        [Test]
        public void Index_ReturnsDefaultView()
        {
            ViewResult result = _unitUnderTest.Index() as ViewResult;

            Assert.AreEqual("BlankPage", result.ViewName);
            Assert.AreEqual("Status 200", result.ViewBag.Header);
            Assert.AreEqual("This is a status 200 page", result.ViewBag.Description);
        }

        [Test]
        public void Status200_ReturnsDefaultView()
        {
            ViewResult result = _unitUnderTest.Status200() as ViewResult;

            Assert.AreEqual("BlankPage", result.ViewName);
            Assert.AreEqual("Status 200", result.ViewBag.Header);
            Assert.AreEqual("This is a status 200 page", result.ViewBag.Description);
        }

        [Test]
        public void Status403_Throws403()
        {
            try
            {
                _unitUnderTest.Status403();
                Assert.Fail();
            }
            catch (HttpException e)
            {
                Assert.AreEqual(403, e.GetHttpCode());
            }
        }

        [Test]
        public void Status404_Throws404()
        {
            try
            {
                _unitUnderTest.Status404();
                Assert.Fail();
            }
            catch (HttpException e)
            {
                Assert.AreEqual(404, e.GetHttpCode());
            }
        }

        [Test]
        public void Status500_Throws500()
        {
            try
            {
                _unitUnderTest.Status500();
                Assert.Fail();
            }
            catch (HttpException e)
            {
                Assert.AreEqual(500, e.GetHttpCode());
            }
        }

        [Test]
        public void Status503_Throws503()
        {
            try
            {
                _unitUnderTest.Status503();
                Assert.Fail();
            }
            catch (HttpException e)
            {
                Assert.AreEqual(503, e.GetHttpCode());
            }
        }

        [Test]
        public void Redirect_301To200()
        {
            RedirectResult result = _unitUnderTest.Redirect(301, 200) as RedirectResult;
            Assert.IsTrue(result.Permanent);
            Assert.AreEqual("/HttpResponse/Status200", result.Url);
        }

        [Test]
        public void Redirect_302To200()
        {
            RedirectResult result = _unitUnderTest.Redirect(302, 200) as RedirectResult;
            Assert.IsFalse(result.Permanent);
            Assert.AreEqual("/HttpResponse/Status200", result.Url);
        }

        [Test]
        public void Redirect_302To404()
        {
            RedirectResult result = _unitUnderTest.Redirect(302, 404) as RedirectResult;
            Assert.IsFalse(result.Permanent);
            Assert.AreEqual("/HttpResponse/Status404", result.Url);
        }

        [Test]
        public void Redirect_InvalidRedirectStatus_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => _unitUnderTest.Redirect(123, 200));
        }

        [Test]
        public void Redirect_InvalidDestinationStatus_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => _unitUnderTest.Redirect(301, 123));
        }
    }
}
