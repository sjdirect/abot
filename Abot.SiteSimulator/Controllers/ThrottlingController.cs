using System;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Controllers
{
    public class ThrottlingController : Controller
    {
        static DateTime? _lastServedDate = null;

        public ActionResult Index()
        {
            return View("Index");
        }

        public ActionResult Handle1RequestEveryXSeconds(int seconds)
        {
            DateTime lastServedDate = _lastServedDate.HasValue ? _lastServedDate.Value : default(DateTime);
            double milliSinceLastRequest = (DateTime.Now - lastServedDate).TotalMilliseconds;

            if (milliSinceLastRequest > ((seconds * 1000) - 1))
            {
                _lastServedDate = DateTime.Now;
                return new HttpResponseController().Status200();
            }

            return new HttpResponseController().Status503();
        }

        public ActionResult ResetLastServcedDate()
        {
            _lastServedDate = default(DateTime);

            return View("BlankPage");
        }
    }
}
