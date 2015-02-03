using System;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Controllers
{
    public class HttpResponseController : Controller
    {
        public ActionResult Index()
        {
            return Status200();
        }

        public ActionResult Status200()
        {
            Thread.Sleep(100);
            ViewBag.Header = "Status 200";
            ViewBag.Description = "This is a status 200 page";
            return View("BlankPage");
        }

        public ActionResult Status403()
        {
            Thread.Sleep(200);
            throw new HttpException(Convert.ToInt32(HttpStatusCode.Forbidden), "");
        }

        public ActionResult Status404()
        {
            Thread.Sleep(300);
            throw new HttpException(Convert.ToInt32(HttpStatusCode.NotFound), "");
        }

        public ActionResult Status500()
        {
            Thread.Sleep(400);
            throw new HttpException(Convert.ToInt32(HttpStatusCode.InternalServerError), "");
        }

        public ActionResult Status503()
        {
            throw new HttpException(Convert.ToInt32(HttpStatusCode.ServiceUnavailable), "");
        }

        public ActionResult Redirect(int redirectHttpStatus, int destinationHttpStatus)
        {
            if(!IsValidRedirectStatus(redirectHttpStatus))
                throw new ArgumentException("redirectHttpStatus is invalid");

            if (!IsValidDestinationStatus(destinationHttpStatus))
                throw new ArgumentException("destinationHttpStatus is invalid");

            return new RedirectResult("/HttpResponse/Status" + destinationHttpStatus, (redirectHttpStatus == 301));
        }

        private bool IsValidRedirectStatus(int status)
        {
            switch (status)
            {
                case 301:
                case 302:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsValidDestinationStatus(int status)
        {
            switch (status)
            {
                case 200:
                case 403:
                case 404:
                case 500:
                case 503:
                    return true;
                default:
                    return false;
            }
        }
    }
}
