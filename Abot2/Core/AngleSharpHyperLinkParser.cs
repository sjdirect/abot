using Abot2.Poco;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;

namespace Abot2.Core
{
    /// <summary>
    /// Parser that uses AngleSharp https://github.com/AngleSharp/AngleSharp to parse page links
    /// </summary>
    public class AngleSharpHyperlinkParser : HyperLinkParser
    {
        public AngleSharpHyperlinkParser()
        {
        }

        public AngleSharpHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanUrlFunc)
            : base(config, cleanUrlFunc)
        {

        }

        protected override string ParserType
        {
            get { return "AngleSharp"; }
        }

        protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
        {
            if (HasRobotsNoFollow(crawledPage))
                return null;

            var hrefValues = crawledPage.AngleSharpHtmlDocument.QuerySelectorAll("a, area")
            .Where(e => !HasRelNoFollow(e))
            .Select(y => y.GetAttribute("href"))
            .Where(a => !string.IsNullOrWhiteSpace(a));

            var canonicalHref = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll("link")
                .Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString()))
                .Select(e => e.GetAttribute("href"));

            return hrefValues.Concat(canonicalHref);
        }

        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            var baseTag = crawledPage.AngleSharpHtmlDocument.QuerySelector("base");
            if (baseTag == null)
                return "";

            var baseTagValue = baseTag.Attributes["href"];
            if (baseTagValue == null)
                return "";

            return baseTagValue.Value.Trim();
        }

        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            var robotsMeta = crawledPage.AngleSharpHtmlDocument
                .QuerySelectorAll("meta[name]")
                .FirstOrDefault(d => d.GetAttribute("name").ToLowerInvariant() == "robots");

            if (robotsMeta == null)
                return "";

            return robotsMeta.GetAttribute("content");
        }

        protected virtual bool HasRelCanonicalPointingToDifferentUrl(IElement e, string orginalUrl)
        {
            return e.HasAttribute("rel") && !string.IsNullOrWhiteSpace(e.GetAttribute("rel")) &&
                    string.Equals(e.GetAttribute("rel"), "canonical", StringComparison.OrdinalIgnoreCase) &&
                    e.HasAttribute("href") && !string.IsNullOrWhiteSpace(e.GetAttribute("href")) &&
                    !string.Equals(e.GetAttribute("href"), orginalUrl, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual bool HasRelNoFollow(IElement e)
        {
            return Config.IsRespectAnchorRelNoFollowEnabled && (e.HasAttribute("rel") && e.GetAttribute("rel").ToLower().Trim() == "nofollow");
        }
    }
}