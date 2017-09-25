using Abot.Poco;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

namespace Abot.Core
{

    /// <summary>
    /// Parser that uses Html Agility Pack http://htmlagilitypack.codeplex.com/ to parse page links
    /// </summary>
    [Serializable]
    public class HapHyperLinkParser : HyperLinkParser
    {
        protected override string ParserType
        {
            get { return "HtmlAgilityPack"; }
        }

        public HapHyperLinkParser()
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
        public HapHyperLinkParser(bool isRespectMetaRobotsNoFollowEnabled,
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

        public HapHyperLinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
            : base(config, cleanURLFunc)
        {
            
        }

        protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
        {
            List<string> hrefValues = new List<string>();
            if (HasRobotsNoFollow(crawledPage))
                return hrefValues;

            HtmlNodeCollection aTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//a[@href]");
            HtmlNodeCollection areaTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//area[@href]");
            HtmlNodeCollection canonicals = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//link[@rel='canonical'][@href]");

            hrefValues.AddRange(GetLinks(aTags));
            hrefValues.AddRange(GetLinks(areaTags));
            hrefValues.AddRange(GetLinks(canonicals));

            return hrefValues;
        }

        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            string hrefValue = "";
            HtmlNode node = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//base");

            //Must use node.InnerHtml instead of node.InnerText since "aaa<br />bbb" will be returned as "aaabbb"
            if (node != null)
                hrefValue = node.GetAttributeValue("href", "").Trim();

            return hrefValue;
        }

        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            string robotsMeta = null;
            HtmlNode robotsNode = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='robots']");
            if (robotsNode != null)
                robotsMeta = robotsNode.GetAttributeValue("content", "");

            return robotsMeta;
        }

        protected virtual List<string> GetLinks(HtmlNodeCollection nodes)
        {
            List<string> hrefs = new List<string>();

            if (nodes == null)
                return hrefs;

            string hrefValue = "";
            foreach (HtmlNode node in nodes)
            {
                if (HasRelNoFollow(node))
                    continue;

                hrefValue = node.Attributes["href"].Value;
                if (!string.IsNullOrWhiteSpace(hrefValue))
                {
                    hrefValue = DeEntitize(hrefValue);
                    hrefs.Add(hrefValue);
                }
            }

            return hrefs;
        }

        protected virtual string DeEntitize(string hrefValue)
        {
            string dentitizedHref = hrefValue;
            
            try
            {
                dentitizedHref = HtmlEntity.DeEntitize(hrefValue);
            }
            catch (Exception)
            {
                _logger.InfoFormat("Error dentitizing uri: {0} This usually means that it contains unexpected characters", hrefValue);
            }

            return dentitizedHref;
        }

        protected virtual bool HasRelNoFollow(HtmlNode node)
        {
            HtmlAttribute attr = node.Attributes["rel"];
            return _config.IsRespectAnchorRelNoFollowEnabled && (attr != null && attr.Value.ToLower().Trim() == "nofollow");
        }
    }
}