﻿using Abot.Poco;
using AngleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
#pragma warning disable 1591

namespace Abot.Core
{
    /// <summary>
    /// Parser that uses AngleSharp https://github.com/AngleSharp/AngleSharp to parse page links
    /// </summary>
    [Serializable]
    public class AngleSharpHyperlinkParser : HyperLinkParser
    {
        public AngleSharpHyperlinkParser()
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
        public AngleSharpHyperlinkParser(bool isRespectMetaRobotsNoFollowEnabled,
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

        public AngleSharpHyperlinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
            : base(config, cleanURLFunc)
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

            IEnumerable<string> hrefValues = crawledPage.AngleSharpHtmlDocument.QuerySelectorAll("a, area")
            .Where(e => !HasRelNoFollow(e))
            .Select(y => y.GetAttribute("href"))
            .Where(a => !string.IsNullOrWhiteSpace(a));

            IEnumerable<string> canonicalHref = crawledPage.AngleSharpHtmlDocument
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
            return _config.IsRespectAnchorRelNoFollowEnabled && (e.HasAttribute("rel") && e.GetAttribute("rel").ToLower().Trim() == "nofollow");
        }
    }
}