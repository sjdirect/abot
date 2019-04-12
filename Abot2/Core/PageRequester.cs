using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abot2.Poco;
using Serilog;

namespace Abot2.Core
{ 
    public interface IPageRequester : IDisposable
    {
        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        Task<CrawledPage> MakeRequestAsync(Uri uri);

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
    }

    public class PageRequester : IPageRequester
    {
        private readonly CrawlConfiguration _config;
        private readonly IWebContentExtractor _contentExtractor;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private readonly HttpClientHandler _httpClientHandler;
        private readonly HttpClient _httpClient;

        public PageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor, HttpClient httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

            if (!_config.IsSslCertificateValidationEnabled)
                ServicePointManager.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;

            if (httpClient == null)
            {
                _httpClientHandler = BuildHttpClientHandler();
                _httpClient = BuildHttpClient(_httpClientHandler);
            }
            else
            {
                _httpClient = httpClient;
            }
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri)
        {
            return await MakeRequestAsync(uri, (x) => new CrawlDecision { Allow = true });
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var crawledPage = new CrawledPage(uri);
            HttpResponseMessage response = null;
            try
            {
                crawledPage.RequestStarted = DateTime.Now;
                using (var requestMessage = BuildHttpRequestMessage(uri))
                {
                    response = await _httpClient.SendAsync(requestMessage, CancellationToken.None);
                }
            }
            catch (HttpRequestException hre)
            {
                crawledPage.HttpRequestException = hre;

                Log.Logger.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, hre);
            }
            catch (Exception e)
            {
                Log.Logger.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, e);
            }
            finally
            {
                crawledPage.HttpRequestMessage = response?.RequestMessage;
                crawledPage.RequestCompleted = DateTime.Now;
                crawledPage.HttpResponseMessage = response;

                try
                {
                    if (response != null)
                    {        
                        var shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                        if (shouldDownloadContentDecision.Allow)
                        {
                            crawledPage.DownloadContentStarted = DateTime.Now;
                            crawledPage.Content = await _contentExtractor.GetContentAsync(response);
                            crawledPage.DownloadContentCompleted = DateTime.Now;
                        }
                        else
                        {
                            Log.Logger.Debug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Debug("Error occurred finalizing requesting url [{0}] {@Exception}", uri.AbsoluteUri, e);
                }
            }

            return crawledPage;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpClientHandler.Dispose();
        }


        protected virtual HttpRequestMessage BuildHttpRequestMessage(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            request.Version = GetEquivalentHttpProtocolVersion();

            return request;
        }


        private HttpClient BuildHttpClient(HttpClientHandler clientHandler)
        {
            var httpClient = new HttpClient(clientHandler);

            httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgentString);
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            if (_config.HttpRequestTimeoutInSeconds > 0)
                httpClient.Timeout = TimeSpan.FromSeconds(_config.HttpRequestTimeoutInSeconds);

            if (_config.IsAlwaysLogin)
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(_config.LoginUser + ":" + _config.LoginPassword));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
            }

            return httpClient;
        }

        private HttpClientHandler BuildHttpClientHandler()
        {
            var httpClientHandler = new HttpClientHandler
            {
                MaxAutomaticRedirections = _config.HttpRequestMaxAutoRedirects,
                UseDefaultCredentials = _config.UseDefaultCredentials
            };

            if (_config.IsHttpRequestAutomaticDecompressionEnabled)
                httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (_config.HttpRequestMaxAutoRedirects > 0)
                httpClientHandler.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;

            if (_config.IsSendingCookiesEnabled)
                httpClientHandler.CookieContainer = _cookieContainer;

            return httpClientHandler;
        }

        private Version GetEquivalentHttpProtocolVersion()
        {
            if (_config.HttpProtocolVersion == HttpProtocolVersion.Version10)
                return HttpVersion.Version10;

            return HttpVersion.Version11;
        }

    }
}
