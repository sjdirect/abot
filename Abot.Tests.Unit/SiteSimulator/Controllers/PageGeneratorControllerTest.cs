using Abot.SiteSimulator.Controllers;
using Abot.SiteSimulator.Models;
using NUnit.Framework;
using System;
using System.Web.Mvc;

namespace Abot.SiteSimulator.Test.Controllers
{
    public class PageGeneratorControllerTest
    {
        PageGeneratorController _unitUnderTest;
        PageSpecs _specs1;

        [SetUp]
        public void SetUp()
        {
            _unitUnderTest = new PageGeneratorController();

            _specs1 = new PageSpecs
            {
                Status200Count = 11,
                Status403Count = 1,
                Status404Count = 2,
                Status500Count = 3,
                Status503Count = 4
            };
        }

        [TearDown]
        public void TearDown()
        {
            _unitUnderTest.ClearCounters();
        }

        [Test]
        public void Index_ReturnsDefaultView()
        {
            ViewResult result = _unitUnderTest.Index() as ViewResult;

            Assert.AreEqual("", result.ViewName);
        }

        [Test]
        public void Generate_ValidPageSpecsString_ReturnsCounts()
        {
            ViewResult result = _unitUnderTest.Generate(_specs1) as ViewResult;
            PageSpecs specs = (PageSpecs)result.Model;

            Assert.AreEqual("GeneratedPage", result.ViewName);
            Assert.AreEqual(11, specs.Status200Count);
            Assert.AreEqual(1, specs.Status403Count);
            Assert.AreEqual(2, specs.Status404Count);
            Assert.AreEqual(3, specs.Status500Count);
            Assert.AreEqual(4, specs.Status503Count);

            //Make sure starting indexes are valid
            Assert.AreEqual(1, specs.Status200StartingIndex);
            Assert.AreEqual(1, specs.Status403StartingIndex);
            Assert.AreEqual(1, specs.Status404StartingIndex);
            Assert.AreEqual(1, specs.Status500StartingIndex);
            Assert.AreEqual(1, specs.Status503StartingIndex);

            //Make sure starting indexes are valid after another method call
            result = _unitUnderTest.Generate(_specs1) as ViewResult;
            specs = (PageSpecs)result.Model;
            Assert.AreEqual(12, specs.Status200StartingIndex);
            Assert.AreEqual(2, specs.Status403StartingIndex);
            Assert.AreEqual(3, specs.Status404StartingIndex);
            Assert.AreEqual(4, specs.Status500StartingIndex);
            Assert.AreEqual(5, specs.Status503StartingIndex);
        }

        [Test]
        public void Generate_EmptyPageSpecs_ReturnsEmptyCounts()
        {
            ViewResult result = _unitUnderTest.Generate(new PageSpecs()) as ViewResult;
            PageSpecs specs = (PageSpecs)result.Model;

            Assert.AreEqual("GeneratedPage", result.ViewName);
            Assert.AreEqual(0, specs.Status200Count);
            Assert.AreEqual(0, specs.Status403Count);
            Assert.AreEqual(0, specs.Status404Count);
            Assert.AreEqual(0, specs.Status500Count);
            Assert.AreEqual(0, specs.Status503Count);

            //Make sure starting indexes are valid
            Assert.AreEqual(1, specs.Status200StartingIndex);
            Assert.AreEqual(1, specs.Status403StartingIndex);
            Assert.AreEqual(1, specs.Status404StartingIndex);
            Assert.AreEqual(1, specs.Status500StartingIndex);
            Assert.AreEqual(1, specs.Status503StartingIndex);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Generate_Status200_NegativeNumber()
        {
            _specs1.Status200Count = -1;
            _unitUnderTest.Generate(_specs1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Generate_Status403_NegativeNumber()
        {
            _specs1.Status403Count = -1;
            _unitUnderTest.Generate(_specs1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Generate_Status404_NegativeNumber()
        {
            _specs1.Status404Count = -1;
            _unitUnderTest.Generate(_specs1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Generate_Status500_NegativeNumber()
        {
            _specs1.Status500Count = -1;
            _unitUnderTest.Generate(_specs1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Generate_Status503_NegativeNumber()
        {
            _specs1.Status503Count = -1;
            _unitUnderTest.Generate(_specs1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Generate_NullPageSpecs()
        {
            _unitUnderTest.Generate(null);
        }

        [Test]
        public void ClearCounters_ReturnsDefaultView()
        {
            //Generate pages to get above zero (We know this works correctly if Generate_ValidPageSpecsString_ParsesStatusAndCount passed)
            ViewResult generatePageResult = _unitUnderTest.Generate(_specs1) as ViewResult;

            //Call CleanCounters method
            ContentResult clearResult = _unitUnderTest.ClearCounters() as ContentResult;
            Assert.AreEqual("Counts Cleared!", clearResult.Content);

            //The next generation of pages should have the counts as if this was the first generation of pages
            generatePageResult = _unitUnderTest.Generate(_specs1) as ViewResult;
            PageSpecs specs = (PageSpecs)generatePageResult.Model;

            specs = (PageSpecs)generatePageResult.Model;
            Assert.AreEqual(11, specs.Status200Count);
            Assert.AreEqual(1, specs.Status403Count);
            Assert.AreEqual(2, specs.Status404Count);
            Assert.AreEqual(3, specs.Status500Count);
            Assert.AreEqual(4, specs.Status503Count);
            Assert.AreEqual(1, specs.Status200StartingIndex);
            Assert.AreEqual(1, specs.Status403StartingIndex);
            Assert.AreEqual(1, specs.Status404StartingIndex);
            Assert.AreEqual(1, specs.Status500StartingIndex);
            Assert.AreEqual(1, specs.Status503StartingIndex);
        }
    }
}