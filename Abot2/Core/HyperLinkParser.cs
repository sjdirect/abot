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

            var links = GetUris(crawledPage, GetHrefValues(crawledPage))
                .Select(hrv => new HyperLink(){ HrefValue = hrv})
                .ToList();
            
            timer.Stop();
            Log.Debug("{0} parsed links from [{1}] in [{2}] milliseconds", ParserType, crawledPage.Uri, timer.ElapsedMilliseconds);

            return links;
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
            var uris = new List<Uri>();

            if (hrefValues == null || !hrefValues.Any())
                return uris;

            //Use the uri of the page that actually responded to the request instead of crawledPage.Uri (Issue 82).
            //Using HttpWebRequest.Address instead of HttpWebResonse.ResponseUri since this is the best practice and mentioned on http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.responseuri.aspx
            var uriToUse = crawledPage.HttpRequestMessage.RequestUri ?? crawledPage.Uri;

            //If html base tag exists use it instead of page uri for relative links
            var baseHref = GetBaseHrefValue(crawledPage);
            if (!string.IsNullOrEmpty(baseHref))
            {
                if (baseHref.StartsWith("//"))
                    baseHref = crawledPage.Uri.Scheme + ":" + baseHref;

                try
                {
                    uriToUse = new Uri(baseHref);
                }
                catch
                {
                    // ignored
                }
            }

            foreach (var hrefValue in hrefValues)
            {
                try
                {
                    // Remove the url fragment part of the url if needed.
                    // This is the part after the # and is often not useful.
                    var href = Config.IsRespectUrlNamedAnchorOrHashbangEnabled
                        ? hrefValue
                        : hrefValue.Split('#')[0];
                    var newUri = new Uri(uriToUse, href);

                    if (CleanUrlFunc != null)
                        newUri = new Uri(CleanUrlFunc(newUri.AbsoluteUri));

                    if (!uris.Exists(u => u.AbsoluteUri == newUri.AbsoluteUri))
                        uris.Add(newUri);
                }
                catch (Exception e)
                {
                    Log.Debug("Could not parse link [{0}] on page [{1}] {@Exception}", hrefValue, crawledPage.Uri, e);
                }
            }

            return uris;
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