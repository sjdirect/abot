using Abot.SiteSimulator.Controllers;
using NUnit.Framework;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Test.Unit.Controllers
{
    [TestFixture]
    public class HomeControllerTest
    {
        [Test]
        public void Index_ReturnsDefaultView()
        {
            ViewResult result = new HomeController().Index() as ViewResult;

            Assert.AreEqual("", result.ViewName);
        }
    }
}
