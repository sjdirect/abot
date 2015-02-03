using Abot.Poco;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Abot.Core
{
    /// <summary>
    /// Handles parsing hyperlinks out of the raw html
    /// </summary>
    public interface IHyperLinkParser
    {
        /// <summary>
        /// Parses html to extract hyperlinks, converts each into an absolute url
        /// </summary>
        IEnumerable<Uri> GetLinks(CrawledPage crawledPage);
    }

    [Serializable]
    public abstract class HyperLinkParser : IHyperLinkParser
    {
        protected ILog _logger = LogManager.GetLogger("AbotLogger");
        protected bool IsRespectMetaRobotsNoFollowEnabled { get; set; }

        public HyperLinkParser()
            :this(false)
        {

        }

        public HyperLinkParser(bool isRespectMetaRobotsNoFollowEnabled)
        {
            IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled;
        }

        /// <summary>
        /// Parses html to extract hyperlinks, converts each into an absolute url
        /// </summary>
        public virtual IEnumerable<Uri> GetLinks(CrawledPage crawledPage)
        {
            CheckParams(crawledPage);

            Stopwatch timer = Stopwatch.StartNew();

            List<Uri> uris = GetUris(crawledPage, GetHrefValues(crawledPage));
            
            timer.Stop();
            _logger.DebugFormat("{0} parsed links from [{1}] in [{2}] milliseconds", ParserType, crawledPage.Uri, timer.ElapsedMilliseconds);

            return uris;
        }

        #region Abstract

        protected abstract string ParserType { get; }

        protected abstract IEnumerable<string> GetHrefValues(CrawledPage crawledPage);

        protected abstract string GetBaseHrefValue(CrawledPage crawledPage);

        protected abstract string GetMetaRobotsValue(CrawledPage crawledPage);

        #endregion

        protected virtual void CheckParams(CrawledPage crawledPage)
        {
            if (crawledPage == null)
                throw new ArgumentNullException("crawledPage");
        }

        protected virtual List<Uri> GetUris(CrawledPage crawledPage, IEnumerable<string> hrefValues)
        {
            List<Uri> uris = new List<Uri>();
            if (hrefValues == null || hrefValues.Count() < 1)
                return uris;

            //Use the uri of the page that actually responded to the request instead of crawledPage.Uri (Issue 82).
            //Using HttpWebRequest.Address instead of HttpWebResonse.ResponseUri since this is the best practice and mentioned on http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.responseuri.aspx
            Uri uriToUse = crawledPage.HttpWebRequest.Address ?? crawledPage.Uri;

            //If html base tag exists use it instead of page uri for relative links
            string baseHref = GetBaseHrefValue(crawledPage);
            if (!string.IsNullOrEmpty(baseHref))
            {
                try
                {
                    uriToUse = new Uri(baseHref);
                }
                catch { }
            }

            string href = "";
            foreach (string hrefValue in hrefValues)
            {
                try
                {
                    href = hrefValue.Split('#')[0];
                    Uri newUri = new Uri(uriToUse, href);

                    if (!uris.Contains(newUri))
                        uris.Add(newUri);
                }
                catch (Exception e)
                {
                    _logger.DebugFormat("Could not parse link [{0}] on page [{1}]", hrefValue, crawledPage.Uri);
                    _logger.Debug(e);
                }
            }

            return uris;
        }

        protected virtual bool HasRobotsNoFollow(CrawledPage crawledPage)
        {
            if (!IsRespectMetaRobotsNoFollowEnabled)
                return false;

            string robotsMeta = robotsMeta = GetMetaRobotsValue(crawledPage);
            bool isRobotsNoFollow = robotsMeta != null &&
                (robotsMeta.ToLower().Contains("nofollow") ||
                robotsMeta.ToLower().Contains("none"));

            if (isRobotsNoFollow)
                _logger.InfoFormat("Robots NoFollow detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);

            return isRobotsNoFollow;
        }
    }
}