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
        protected CrawlConfiguration _config;
        protected Func<string, string> _cleanURLFunc;

        protected HyperLinkParser()
            :this(new CrawlConfiguration(), null)
        {

        }

        protected HyperLinkParser(CrawlConfiguration config, Func<string, string> cleanURLFunc)
        {
            _config = config;
            _cleanURLFunc = cleanURLFunc;
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
                if (baseHref.StartsWith("//"))
                    baseHref = crawledPage.Uri.Scheme + ":" + baseHref;

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
                    // Remove the url fragment part of the url if needed.
                    // This is the part after the # and is often not useful.
                    href = _config.IsRespectUrlNamedAnchorOrHashbangEnabled
                        ? hrefValue
                        : hrefValue.Split('#')[0];
                    Uri newUri = new Uri(uriToUse, href);

                    if (_cleanURLFunc != null)
                        newUri = new Uri(_cleanURLFunc(newUri.AbsoluteUri));

                    if (!uris.Exists(u => u.AbsoluteUri == newUri.AbsoluteUri))
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
            //X-Robots-Tag http header
            if(_config.IsRespectHttpXRobotsTagHeaderNoFollowEnabled)
            {
                var xRobotsTagHeader = crawledPage.HttpWebResponse.Headers["X-Robots-Tag"];
                if (xRobotsTagHeader != null && 
                    (xRobotsTagHeader.ToLower().Contains("nofollow") ||
                     xRobotsTagHeader.ToLower().Contains("none")))
                {
                    _logger.InfoFormat("Http header X-Robots-Tag nofollow detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
                    return true;
                }   
            }

            //Meta robots tag
            if (_config.IsRespectMetaRobotsNoFollowEnabled)
            {
                string robotsMeta = GetMetaRobotsValue(crawledPage);
                if (robotsMeta != null &&
                    (robotsMeta.ToLower().Contains("nofollow") ||
                     robotsMeta.ToLower().Contains("none")))
                {
                    _logger.InfoFormat("Meta Robots nofollow tag detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
                    return true;
                }                
                
            }

            return false;
        }
    }
}