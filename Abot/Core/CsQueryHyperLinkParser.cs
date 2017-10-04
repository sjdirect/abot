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
    [Obsolete("CSQuery is no longer actively maintained. Use AngleSharpHyperlinkParser for similar usage/functionality")]
    public class CSQueryHyperlinkParser : HyperLinkParser
    {
        public CSQueryHyperlinkParser()
            :base()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isRespectMetaRobotsNoFollowEnabled">Whether parser should ignore pages with meta no robots</param>
        /// <param name="isRespectAnchorRelNoFollowEnabled">Whether parser should ignore links with rel no follow</param>
        /// <param name="cleanURLFunc">Function to clean the url</param>
        /// <param name="isRespectUrlNamedAnchorOrHashbangEnabled">Whether parser should consider named anchor and/or hashbang '#' character as part of the url</param>
        [Obsolete("Use the constructor that accepts a configuration object instead")]
        public CSQueryHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled,
                                  bool isRespectAnchorRelNoFollowEnabled,
                                  Func<string, string> cleanURLFunc = null,
                                  bool isRespectUrlNamedAnchorOrHashbangEnabled = false)
            :this(new CrawlConfiguration
            {
                IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled,
                IsRespectUrlNamedAnchorOrHashbangEnabled = isRespectUrlNamedAnchorOrHashbangEnabled,
                IsRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled
            }, cleanURLFunc)
        {

        }

        public CSQueryHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
            : base(config, cleanURLFunc)
        {

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
            .Select(y => y.GetAttribute("href"))
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
            return _config.IsRespectAnchorRelNoFollowEnabled && (e.HasAttribute("rel") && e.GetAttribute("rel").ToLower().Trim() == "nofollow");
        }
    }
}