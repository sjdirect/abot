using Abot2.Poco;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Abot2.Core
{
    /// <summary>
    /// Handles parsing html out of the raw html
    /// </summary>
    public interface IHtmlParser
    {
        /// <summary>
        /// Parses html to extract hyperlinks, converts each into an absolute url representation.
        /// </summary>
        IEnumerable<HyperLink> GetLinks(CrawledPage crawledPage);
    }

    public abstract class HyperLinkParser : IHtmlParser
    {
        protected CrawlConfiguration Config;
        protected Func<string, string> CleanUrlFunc;

        protected HyperLinkParser()
            :this(new CrawlConfiguration(), null)
        {

        }

        protected HyperLinkParser(CrawlConfiguration config, Func<string, string> cleanUrlFunc)
        {
            Config = config;
            CleanUrlFunc = cleanUrlFunc;
        }

        /// <summary>
        /// Parses html to extract hyperlinks, converts each into an absolute url
        /// </summary>
        public virtual IEnumerable<HyperLink> GetLinks(CrawledPage crawledPage)
        {
            CheckParams(crawledPage);

            var timer = Stopwatch.StartNew();

            var links = GetHyperLinks(crawledPage, GetRawHyperLinks(crawledPage));

            timer.Stop();
            Log.Debug("{0} parsed links from [{1}] in [{2}] milliseconds", ParserType, crawledPage.Uri, timer.ElapsedMilliseconds);

            return links;
        }

        #region Abstract

        protected abstract string ParserType { get; }

        protected abstract IEnumerable<HyperLink> GetRawHyperLinks(CrawledPage crawledPage);

        protected abstract string GetBaseHrefValue(CrawledPage crawledPage);

        protected abstract string GetMetaRobotsValue(CrawledPage crawledPage);

        #endregion

        protected virtual void CheckParams(CrawledPage crawledPage)
        {
            if (crawledPage == null)
                throw new ArgumentNullException("crawledPage");
        }

        protected virtual IEnumerable<HyperLink> GetHyperLinks(CrawledPage crawledPage, IEnumerable<HyperLink> rawLinks)
        {
            var finalList = new List<HyperLink>();
            if (rawLinks == null || !rawLinks.Any())
                return finalList;

            //Use the uri of the page that actually responded to the request instead of crawledPage.Uri (Issue 82).
            var uriToUse = crawledPage.HttpRequestMessage.RequestUri ?? crawledPage.Uri;

            //If html base tag exists use it instead of page uri for relative links
            var baseHref = GetBaseHrefValue(crawledPage);
            if (!string.IsNullOrEmpty(baseHref))
            {
                if (baseHref.StartsWith("//"))
                    baseHref = crawledPage.Uri.Scheme + ":" + baseHref;
                else if (baseHref.StartsWith("/"))
                    // '/' points to the root of the filesystem when running on Linux, and is as such
                    // considered an absolute URI
                    baseHref = uriToUse.GetLeftPart(UriPartial.Authority) + baseHref;

                if (Uri.TryCreate(uriToUse, baseHref, out Uri baseUri))
                    uriToUse = baseUri;
            }

            foreach (var rawLink in rawLinks)
            {
                try
                {
                    // Remove the url fragment part of the url if needed.
                    // This is the part after the # and is often not useful.
                    var href = Config.IsRespectUrlNamedAnchorOrHashbangEnabled
                        ? rawLink.RawHrefValue
                        : rawLink.RawHrefValue.Split('#')[0];

                    var uriValueToUse = (CleanUrlFunc != null) ? new Uri(CleanUrlFunc(new Uri(uriToUse, href).AbsoluteUri)) : new Uri(uriToUse, href);
                    
                    //rawLink is copied and setting its value directly is not reflected in the collection, must create another object
                    finalList.Add(
                        new HyperLink
                        {
                            RawHrefValue = rawLink.RawHrefValue,
                            RawHrefText = rawLink.RawHrefText,
                            HrefValue = uriValueToUse
                        });
                }
                catch (Exception e)
                {
                    Log.Debug("Could not parse link [{0}] on page [{1}] {@Exception}", rawLink.RawHrefValue, crawledPage.Uri, e);
                }
            }

            return finalList.Distinct();
        }

        protected virtual bool HasRobotsNoFollow(CrawledPage crawledPage)
        {
            //X-Robots-Tag http header
            if(Config.IsRespectHttpXRobotsTagHeaderNoFollowEnabled)
            {
                IEnumerable<string> xRobotsTagHeaderValues;
                if (!crawledPage.HttpResponseMessage.Headers.TryGetValues("X-Robots-Tag", out xRobotsTagHeaderValues))
                    return false;
                
                var xRobotsTagHeader = xRobotsTagHeaderValues.ElementAt(0);
                if (xRobotsTagHeader != null && 
                    (xRobotsTagHeader.ToLower().Contains("nofollow") ||
                     xRobotsTagHeader.ToLower().Contains("none")))
                {
                    Log.Information("Http header X-Robots-Tag nofollow detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
                    return true;
                }   
            }

            //Meta robots tag
            if (Config.IsRespectMetaRobotsNoFollowEnabled)
            {
                var robotsMeta = GetMetaRobotsValue(crawledPage);
                if (robotsMeta != null &&
                    (robotsMeta.ToLower().Contains("nofollow") ||
                     robotsMeta.ToLower().Contains("none")))
                {
                    Log.Information("Meta Robots nofollow tag detected on uri [{0}], will not crawl links on this page.", crawledPage.Uri);
                    return true;
                }                
                
            }

            return false;
        }
    }
}