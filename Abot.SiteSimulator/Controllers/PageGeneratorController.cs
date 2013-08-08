using Abot.SiteSimulator.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Controllers
{
    public class PageGeneratorController : Controller
    {
        static Dictionary<int, int> _generatedPagesCounts = new Dictionary<int, int>();
        static DateTime refreshCountsTime;

        static PageGeneratorController()
        {
            InitializeCounts();
        }

        private static void InitializeCounts()
        {
            _generatedPagesCounts = new Dictionary<int, int>();
            _generatedPagesCounts.Add(200, 0);
            _generatedPagesCounts.Add(403, 0);
            _generatedPagesCounts.Add(404, 0);
            _generatedPagesCounts.Add(500, 0);
            _generatedPagesCounts.Add(503, 0);

            refreshCountsTime = DateTime.Now.AddSeconds(30);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Generate(PageSpecs pageSpecs)
        {
            if (pageSpecs == null)
                throw new ArgumentNullException("pageSpecs");

            if (pageSpecs.Status200Count < 0)
                throw new ArgumentException("pageSpecs.Status200 cannot be negative");

            if (pageSpecs.Status403Count < 0)
                throw new ArgumentException("pageSpecs.Status403 cannot be negative");

            if (pageSpecs.Status404Count < 0)
                throw new ArgumentException("pageSpecs.Status404 cannot be negative");

            if (pageSpecs.Status500Count < 0)
                throw new ArgumentException("pageSpecs.Status500 cannot be negative");

            if (pageSpecs.Status503Count < 0)
                throw new ArgumentException("pageSpecs.Status503 cannot be negative");

            if (DateTime.Now > refreshCountsTime)
                InitializeCounts();

            PageSpecs finalSpecs = new PageSpecs();

            lock (_generatedPagesCounts)
            {
                finalSpecs.Status200Count = pageSpecs.Status200Count;
                _generatedPagesCounts[200] += pageSpecs.Status200Count;
                finalSpecs.Status200StartingIndex = (_generatedPagesCounts[200] - pageSpecs.Status200Count) + 1;

                finalSpecs.Status403Count = pageSpecs.Status403Count;
                _generatedPagesCounts[403] += pageSpecs.Status403Count;
                finalSpecs.Status403StartingIndex = (_generatedPagesCounts[403] - pageSpecs.Status403Count) + 1;

                finalSpecs.Status404Count = pageSpecs.Status404Count;
                _generatedPagesCounts[404] += pageSpecs.Status404Count;
                finalSpecs.Status404StartingIndex = (_generatedPagesCounts[404] - pageSpecs.Status404Count) + 1;

                finalSpecs.Status500Count = pageSpecs.Status500Count;
                _generatedPagesCounts[500] += pageSpecs.Status500Count;
                finalSpecs.Status500StartingIndex = (_generatedPagesCounts[500] - pageSpecs.Status500Count) + 1;

                finalSpecs.Status503Count = pageSpecs.Status503Count;
                _generatedPagesCounts[503] += pageSpecs.Status503Count;
                finalSpecs.Status503StartingIndex = (_generatedPagesCounts[503] - pageSpecs.Status503Count) + 1;
            }

            return View("GeneratedPage", finalSpecs);
        }

        public ActionResult ClearCounters()
        {
            InitializeCounts();

            return Content("Counts Cleared!");
        }
    }
}
