using Abot.Poco;
using CsQuery;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Abot.Core
{
    /// <summary>
    /// Parser that uses CsQuery https://github.com/jamietre/CsQuery to parse page links
    /// </summary>
    [Serializable]
    public class CSQueryHyperlinkParser : HyperLinkParser
    {
        Func<string, string> _cleanURLFunc;
        bool _isRespectMetaRobotsNoFollowEnabled;
        bool _isRespectAnchorRelNoFollowEnabled;
        
        public CSQueryHyperlinkParser()
        {
        }

        public CSQueryHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanURLFunc = null)
            : base(isRespectMetaRobotsNoFollowEnabled)
        {
            _isRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled;
            _isRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled;
            _cleanURLFunc = cleanURLFunc;
        }
        
        protected override string ParserType
        {
            get { return "CsQuery"; }
        }

        protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
        {
            if (HasRobotsNoFollow(crawledPage))
                return null;

            IEnumerable<string> hrefValues = crawledPage.CsQueryDocument.Select("a, area")
            .Elements
            .Where(e => !HasRelNoFollow(e))
            .Select(y => _cleanURLFunc != null ? _cleanURLFunc(y.GetAttribute("href")) : y.GetAttribute("href"))
            .Where(a => !string.IsNullOrWhiteSpace(a));

            IEnumerable<string> canonicalHref = crawledPage.CsQueryDocument.
                Select("link").Elements.
                Where(e => HasRelCanonicalPointingToDifferentUrl(e, crawledPage.Uri.ToString())).
                Select(e => e.Attributes["href"]);

            return hrefValues.Concat(canonicalHref);
        }

        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            string baseTagValue = crawledPage.CsQueryDocument.Select("base").Attr("href") ?? "";
            return baseTagValue.Trim();
        }

        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            return crawledPage.CsQueryDocument["meta[name]"].Filter(d => d.Name.ToLowerInvariant() == "robots").Attr("content");
        }

        protected virtual bool HasRelCanonicalPointingToDifferentUrl(IDomElement e, string orginalUrl)
        {
            return e.HasAttribute("rel") && !string.IsNullOrWhiteSpace(e.Attributes["rel"]) &&
                    string.Equals(e.Attributes["rel"], "canonical", StringComparison.OrdinalIgnoreCase) &&
                    e.HasAttribute("href") && !string.IsNullOrWhiteSpace(e.Attributes["href"]) &&
                    !string.Equals(e.Attributes["href"], orginalUrl, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual bool HasRelNoFollow(IDomElement e)
        {
            return _isRespectAnchorRelNoFollowEnabled && (e.HasAttribute("rel") && e.GetAttribute("rel").ToLower().Trim() == "nofollow");
        }
    }
}