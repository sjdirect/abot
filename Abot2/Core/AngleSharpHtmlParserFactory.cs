using System;
using AngleSharp.Html.Parser;
using IAngleSharpHtmlParser = AngleSharp.Html.Parser.IHtmlParser;

namespace Abot2.Core
{
    /// <summary>
    /// Creates the AngleSharp parser that parses a crawled page into CrawledPage.AngleSharpHtmlDocument
    /// </summary>
    public interface IAngleSharpHtmlParserFactory
    {
        /// <summary>
        /// Creates a parser for one page. Pages can be parsed on several threads at the same time, so return a parser that isn't
        /// used by other threads, unless you know it is safe to share.
        /// </summary>
        IAngleSharpHtmlParser Create();
    }

    /// <summary>
    /// Creates a new AngleSharp HtmlParser with the given options, and its own browsing context, for every page
    /// </summary>
    public class AngleSharpHtmlParserFactory : IAngleSharpHtmlParserFactory
    {
        /// <summary>
        /// The factory Abot uses unless you pass in your own. It creates parsers with DefaultOptions.
        /// </summary>
        public static AngleSharpHtmlParserFactory Default { get; } = new AngleSharpHtmlParserFactory();

        /// <summary>
        /// The parser options Abot uses by default: AngleSharp's defaults, except that the parser's own depth limit is lifted
        /// (MaximumDepth = Int32.MaxValue) as it was before AngleSharp 1.8.6. Past its limit AngleSharp attaches elements as siblings,
        /// which can put text out of source order; CrawledPage limits the depth itself and keeps document order (see
        /// CrawledPage.MaxHtmlDocumentDepth). Each read returns a new copy, so you can change it and pass it to the constructor.
        /// </summary>
        public static HtmlParserOptions DefaultOptions => new HtmlParserOptions { MaximumDepth = Int32.MaxValue };

        /// <summary>
        /// Creates a factory that uses DefaultOptions
        /// </summary>
        public AngleSharpHtmlParserFactory()
            : this(DefaultOptions)
        {
        }

        /// <summary>
        /// Creates a factory that uses the given options. A MaximumDepth that isn't set means AngleSharp's limit of 512, which can
        /// put text of very deeply nested pages out of source order; start from DefaultOptions to keep Abot's behaviour.
        /// </summary>
        public AngleSharpHtmlParserFactory(HtmlParserOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// The options every parser is created with
        /// </summary>
        public HtmlParserOptions Options { get; }

        /// <inheritdoc />
        public virtual IAngleSharpHtmlParser Create() => new HtmlParser(Options);
    }
}
