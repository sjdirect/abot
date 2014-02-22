using Abot.Poco;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

namespace Abot.Core
{

    /// <summary>
    /// Parser that uses Html Agility Pack http://htmlagilitypack.codeplex.com/ to parse page links
    /// </summary>
    public class HapHyperLinkParser : HyperLinkParser
    {
        Func<string, string> _cleanURLFunc;
        bool _isRespectMetaRobotsNoFollowEnabled;
        bool _isRespectAnchorRelNoFollowEnabled;

        protected override string ParserType
        {
            get { return "HtmlAgilityPack"; }
        }

        public HapHyperLinkParser()
            :this(false, false)
        {
        }

        public HapHyperLinkParser(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanURLFunc = null)
        {
            _isRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled;
            _isRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled;
            _cleanURLFunc = cleanURLFunc;
        }

        protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
        {
            List<string> hrefValues = new List<string>();
            if (HasRobotsNoFollow(crawledPage))
                return hrefValues;

            HtmlNodeCollection aTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//a[@href]");
            HtmlNodeCollection areaTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//area[@href]");

            hrefValues.AddRange(GetLinks(aTags));
            hrefValues.AddRange(GetLinks(areaTags));

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

        private List<string> GetLinks(HtmlNodeCollection nodes)
        {
            List<string> hrefs = new List<string>();

            if (nodes == null)
                return hrefs;

            string hrefValue = "";
            foreach (HtmlNode node in nodes)
            {
                if (HasRelNoFollow(node))
                    continue;

                hrefValue = _cleanURLFunc != null ? _cleanURLFunc(node.Attributes["href"].Value) : node.Attributes["href"].Value;
                if (!string.IsNullOrWhiteSpace(hrefValue))
                {
                    hrefValue = DeEntitize(hrefValue);
                    hrefs.Add(hrefValue);
                }	
            }

            return hrefs;
        }

        private string DeEntitize(string hrefValue)
        {
            string dentitizedHref = hrefValue;
            
            try
            {
                dentitizedHref = HtmlEntity.DeEntitize(hrefValue);
            }
            catch (Exception e)
            {
                _logger.InfoFormat("Error dentitizing uri: {0} This usually means that it contains unexpected characters. Message: {1}", hrefValue, e.Message);
            }

            return dentitizedHref;
        }

        private bool HasRobotsNoFollow(CrawledPage crawledPage)
        {
            string robotsMeta = null;
            if (_isRespectMetaRobotsNoFollowEnabled)
            {
                HtmlNode robotsNode = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='robots']");
                if (robotsNode != null)
                    robotsMeta = robotsNode.GetAttributeValue("content", "");
            }

            return robotsMeta != null && robotsMeta.ToLower().Contains("nofollow");
        }

        private bool HasRelNoFollow(HtmlNode node)
        {
            HtmlAttribute attr = node.Attributes["rel"];
            return _isRespectAnchorRelNoFollowEnabled && (attr != null && attr.Value.ToLower().Trim() == "nofollow");
        }
    }
}