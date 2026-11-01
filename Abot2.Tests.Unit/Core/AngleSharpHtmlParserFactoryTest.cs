using System;
using Abot2.Core;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class AngleSharpHtmlParserFactoryTest
    {
        [TestMethod]
        public void DefaultOptions_AreTheParsersDefaultsWithoutDepthLimit()
        {
            //What CrawledPage used before the factory: new HtmlParser()
            var expected = new HtmlParser().Options;
            expected.MaximumDepth = Int32.MaxValue;

            Assert.AreEqual(expected, AngleSharpHtmlParserFactory.DefaultOptions);
        }

        [TestMethod]
        public void DefaultOptions_ChangingTheReturnedCopy_DoesNotChangeTheDefault()
        {
            var options = AngleSharpHtmlParserFactory.DefaultOptions;
            options.IsScripting = true;
            options.MaximumDepth = 10;

            Assert.IsFalse(AngleSharpHtmlParserFactory.DefaultOptions.IsScripting);
            Assert.AreEqual(Int32.MaxValue, AngleSharpHtmlParserFactory.DefaultOptions.MaximumDepth);
            Assert.AreEqual(Int32.MaxValue, AngleSharpHtmlParserFactory.Default.Options.MaximumDepth);
        }

        [TestMethod]
        public void Default_UsesDefaultOptions()
        {
            Assert.AreEqual(AngleSharpHtmlParserFactory.DefaultOptions, AngleSharpHtmlParserFactory.Default.Options);
            Assert.AreEqual(AngleSharpHtmlParserFactory.DefaultOptions, new AngleSharpHtmlParserFactory().Options);
        }

        [TestMethod]
        public void Create_OptionsPassedIn_CreatesParsersWithThem()
        {
            var options = new HtmlParserOptions { IsScripting = true, MaximumDepth = 100 };
            var unitUnderTest = new AngleSharpHtmlParserFactory(options);

            var parser = (HtmlParser)unitUnderTest.Create();

            Assert.AreEqual(options, parser.Options);
        }

        [TestMethod]
        public void Create_CalledTwice_ReturnsANewParserEachTime()
        {
            var unitUnderTest = new AngleSharpHtmlParserFactory();

            Assert.AreNotSame(unitUnderTest.Create(), unitUnderTest.Create());
        }
    }
}
