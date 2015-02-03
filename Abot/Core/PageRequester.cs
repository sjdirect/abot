﻿using Abot.Poco;
using log4net;
using System;
using System.CodeDom;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

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

        ///// <summary>
        ///// Asynchronously make an http web request to the url and download its content based on the param func decision
        ///// </summary>
        //Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
    }

    [Serializable]
    public class PageRequester : IPageRequester
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");

        protected CrawlConfiguration _config;
        protected IWebContentExtractor _extractor;
        protected CookieContainer container = new CookieContainer();

        public PageRequester(CrawlConfiguration config)
            : this(config, null)
        {

        }

        public PageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _config = config;

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

            _extractor = contentExtractor ?? new WebContentExtractor();
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
                crawledPage.RequestStarted = DateTime.Now;
                response = (HttpWebResponse)request.GetResponse();
                ProcessResponseObject(response);
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
                crawledPage.RequestCompleted = DateTime.Now;
                if (response != null)
                {
                    crawledPage.HttpWebResponse = response;
                    CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                    if (shouldDownloadContentDecision.Allow)
                    {
                        crawledPage.DownloadContentStarted = DateTime.Now;
                        crawledPage.Content = _extractor.GetContent(response);
                        crawledPage.DownloadContentCompleted = DateTime.Now;
                    }
                    else
                    {
                        _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);    
                    }

                    response.Close();//Should already be closed by _extractor but just being safe
                }
            }

            return crawledPage;
        }

        ///// <summary>
        ///// Asynchronously make an http web request to the url and download its content based on the param func decision
        ///// </summary>
        //public Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        //{
        //    if (uri == null)
        //        throw new ArgumentNullException("uri");

        //    CrawledPage crawledPage = new CrawledPage(uri);
        //    crawledPage.RequestStarted = DateTime.Now;

        //    HttpWebRequest request = BuildRequestObject(uri);
        //    HttpWebResponse response = null;

        //    crawledPage.HttpWebRequest = request;
        //    crawledPage.RequestStarted = DateTime.Now;

        //    Task<WebResponse> task = Task.Factory.FromAsync(
        //        request.BeginGetResponse,
        //        asyncResult => request.EndGetResponse(asyncResult),
        //        null);

        //    return task.ContinueWith((Task<WebResponse> t) =>
        //    {
        //        crawledPage.RequestCompleted = DateTime.Now;

        //        if (t.IsFaulted)
        //        {
        //            //handle error
        //            Exception firstException = t.Exception.InnerExceptions.First();
        //            crawledPage.WebException = firstException as WebException;

        //            if (crawledPage.WebException != null && crawledPage.WebException.Response != null)
        //                response = (HttpWebResponse)crawledPage.WebException.Response;
                    
        //            _logger.DebugFormat("Error occurred requesting url [{0}]", uri.AbsoluteUri);
        //            _logger.Debug(crawledPage.WebException);
        //        }
        //        else
        //        {
        //            ProcessResponseObject(response);
        //            response = (HttpWebResponse)t.Result;
        //        }

        //        if (response != null)
        //        {
        //            crawledPage.HttpWebResponse = response;
        //            CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
        //            if (shouldDownloadContentDecision.Allow)
        //            {
        //                crawledPage.DownloadContentStarted = DateTime.Now;
        //                crawledPage.Content = _extractor.GetContent(response);
        //                crawledPage.DownloadContentCompleted = DateTime.Now;
        //            }
        //            else
        //            {
        //                _logger.DebugFormat("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri,
        //                    shouldDownloadContentDecision.Reason);
        //            }

        //            response.Close(); //Should already be closed by _extractor but just being safe
        //        }

        //        return crawledPage;
        //    });
        //}

        protected virtual HttpWebRequest BuildRequestObject(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;
            request.UserAgent = _config.UserAgentString;
            request.Accept = "*/*";

            if(_config.HttpRequestMaxAutoRedirects > 0)
                request.MaximumAutomaticRedirections = _config.HttpRequestMaxAutoRedirects;

            if (_config.IsHttpRequestAutomaticDecompressionEnabled)
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if(_config.HttpRequestTimeoutInSeconds > 0)
                request.Timeout = _config.HttpRequestTimeoutInSeconds * 1000;

            if (_config.IsSendingCookiesEnabled)
                request.CookieContainer = container;

            return request;
        }

        protected virtual void ProcessResponseObject(HttpWebResponse response)
        {
            if (response != null && _config.IsSendingCookiesEnabled)
            {
                CookieCollection cookies = response.Cookies;
                container.Add(cookies);
            }
        }
    }
}