using Abot.Poco;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace Abot.Core
{
    public interface IPageRequester
    {
        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        CrawledPage MakeRequest(Uri uri);

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
    }

    public class PageRequester : IPageRequester
    {
        static ILog _logger = LogManager.GetLogger(typeof(PageRequester).FullName);

        protected CrawlConfiguration _config;
        protected string _userAgentString;

        public PageRequester(CrawlConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _userAgentString = config.UserAgentString.Replace("@ABOTASSEMBLYVERSION@", Assembly.GetAssembly(this.GetType()).GetName().Version.ToString());
            _config = config;

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual CrawledPage MakeRequest(Uri uri)
        {
            return MakeRequest(uri, (x) => new CrawlDecision { Allow = true });
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            CrawledPage crawledPage = new CrawledPage(uri);

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = BuildRequestObject(uri);
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                crawledPage.WebException = e;

                if (e.Response != null)
                    response = (HttpWebResponse)e.Response;

                _logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
                _logger.Debug(e);
            }
            catch (Exception e)
            {
                _logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
                _logger.Debug(e);
            }
            finally
            {
                crawledPage.HttpWebRequest = request;

                if (response != null)
                {
                    crawledPage.HttpWebResponse = response;
                    CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                    if (shouldDownloadContentDecision.Allow)
                    {
                        crawledPage.RawContent = GetRawHtml(response, uri);
                        crawledPage.PageSizeInBytes = Encoding.UTF8.GetBytes(crawledPage.RawContent).Length;
                    }
                    else
                    {
                        _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                    }
                    response.Close();
                }
            }
            
            return crawledPage;
        }

        protected virtual HttpWebRequest BuildRequestObject(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;
            request.UserAgent = _userAgentString;
            request.Accept = "*/*";

            if(_config.HttpRequestMaxAutoRedirects > 0)
                request.MaximumAutomaticRedirections = _config.HttpRequestMaxAutoRedirects;

            if (_config.IsHttpRequestAutomaticDecompressionEnabled)
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if(_config.HttpRequestTimeoutInSeconds > 0)
                request.Timeout = _config.HttpRequestTimeoutInSeconds * 1000;

            return request;
        }

        protected virtual string GetRawHtml(HttpWebResponse response, Uri requestUri)
        {
            string rawHtml = "";
            try
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    rawHtml = sr.ReadToEnd();
                    sr.Close();
                }
            }
            catch (Exception e)
            {
                _logger.WarnFormat("Error occurred while downloading content of url {0}", requestUri.AbsoluteUri);
                _logger.Warn(e);
            }

            return rawHtml;
        }
    }
}