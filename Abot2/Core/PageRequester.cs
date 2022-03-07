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

        // State used when re-using HttpClient instances
        private static bool? _reuseHttpClient = null;
        private static HttpClient _savedHttpClient = null;
        private static HttpClientHandler _savedHttpHandler = null;
        private static object _syncObject = new object();

        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;

        public PageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor, HttpClient httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (_reuseHttpClient.HasValue)
            {
                if (config.ReUseHttpClientInstance != _reuseHttpClient.Value)
                    throw new ArgumentException("Setting reuse of HttpClient instances is global for all PageRequests and cannot be updated after the initial configuration has been set.", nameof(config));
            }
            else
            {
                _reuseHttpClient = config.ReUseHttpClientInstance;
            }

            _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

            _httpClient = httpClient;

            if (_reuseHttpClient.Value && httpClient != null)
            {
                if (_savedHttpClient == null)
                {
                    // If this path is taken, we cannot know the httpClientHandler used to construct the given httpClient. There are no public accessors for it
                    _savedHttpHandler = null;
                }

                if (_savedHttpClient?.Equals(_httpClient) == false)
                {
                    // Note: We don't own the passed in httpClient but it should likely be disposed. This may leak the old httpclient for a while 
                    // since the caller that owned it is unaware we are no longer referencing it.
                    Log.Debug($"Reuse of HttpClient is enabled, but a different instance was passed into a PageRequester constructor. Param: {nameof(httpClient)}");
                }

                _savedHttpClient = httpClient;
            }
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri)
        {
            return await MakeRequestAsync(uri, (x) => new CrawlDecision { Allow = true }).ConfigureAwait(false);
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            // If other threads are running page requests we must protect this initialization
            lock (_syncObject)
            {
                if (_httpClient == null)
                {
                    // If user passed in a preconstructed HttpClient to PageRequester constructor, this _httpClientHandler is NOT the one being used by _httpClient - it's unused.
                    _httpClientHandler = BuildHttpClientHandler(uri);
                    _httpClient = BuildHttpClient(_httpClientHandler);
                }
            }

            var crawledPage = new CrawledPage(uri);
            HttpResponseMessage response = null;
            try
            {
                crawledPage.RequestStarted = DateTime.Now;
                using (var requestMessage = BuildHttpRequestMessage(uri))
                {
                    response = await _httpClient.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);
                }

                var statusCode = Convert.ToInt32(response.StatusCode);
                if (statusCode < 200 || statusCode > 399)
                    throw new HttpRequestException($"Server response was unsuccessful, returned [http {statusCode}]");
            }
            catch (HttpRequestException hre)
            {
                crawledPage.HttpRequestException = hre;
                Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, hre);
            }
            catch (TaskCanceledException ex)
            {
                crawledPage.HttpRequestException = new HttpRequestException("Request timeout occurred", ex);//https://stackoverflow.com/questions/10547895/how-can-i-tell-when-httpclient-has-timed-out
                Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, crawledPage.HttpRequestException);
            }
            catch (Exception e)
            {
                crawledPage.HttpRequestException = new HttpRequestException("Unknown error occurred", e);
                Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, crawledPage.HttpRequestException);
            }
            finally
            {
                crawledPage.HttpRequestMessage = response?.RequestMessage;
                crawledPage.RequestCompleted = DateTime.Now;
                crawledPage.HttpResponseMessage = response;
                crawledPage.HttpClientHandler = _httpClientHandler;

                try
                {
                    if (response != null)
                    {        
                        var shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                        if (shouldDownloadContentDecision.Allow)
                        {
                            crawledPage.DownloadContentStarted = DateTime.Now;
                            crawledPage.Content = await _contentExtractor.GetContentAsync(response).ConfigureAwait(false);
                            crawledPage.DownloadContentCompleted = DateTime.Now;
                        }
                        else
                        {
                            Log.Debug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Debug("Error occurred finalizing requesting url [{0}] {@Exception}", uri.AbsoluteUri, e);
                }
            }

            return crawledPage;
        }

        public void Dispose()
        {
            // When re-using httpclient instances, just clear the instance refs, do not dispose
            if (!_reuseHttpClient.Value)
            {
                _httpClient?.Dispose();
                _httpClientHandler?.Dispose();
            }
            _httpClient = null;
            _httpClientHandler = null;
        }

        // If PageRequester has re-used an httpclient, these should be cleaned up
        // If you enable re-use and you do not call this method, your HttpClient will leak
        public static void DisposeReusedObjects()
        {
            if (_reuseHttpClient.HasValue)
            {
                _reuseHttpClient = null;
                _savedHttpHandler?.Dispose();
                _savedHttpClient?.Dispose();
                _savedHttpHandler = null;
                _savedHttpClient = null;
            }
        }
        
        
        protected virtual HttpRequestMessage BuildHttpRequestMessage(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            
            request.Version = GetEquivalentHttpProtocolVersion();
            
            return request;
        }
        
        protected virtual HttpClient BuildHttpClient(HttpClientHandler clientHandler)
        {
            // Return the shared httpclient instance if re-using
            if (_reuseHttpClient.Value)
            {
                if (_savedHttpClient != null)
                    return _savedHttpClient;
            }

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

            // This instance will be the one to be be shared (when re-use enabled)
            _savedHttpClient = httpClient;

            return httpClient;
        }

        protected virtual HttpClientHandler BuildHttpClientHandler(Uri rootUri)
        {
            if(rootUri == null)
                throw new ArgumentNullException(nameof(rootUri));

            // If reusing connections, only build new handler if we are building the shared HttpClient instance
            if (_reuseHttpClient.Value)
            {
                // If we already have existing client, no handler is required
                if (_savedHttpHandler != null)
                    return _savedHttpHandler;
            }

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
            {
                httpClientHandler.CookieContainer = _cookieContainer;
                httpClientHandler.UseCookies = true;
            }

            if (!_config.IsSslCertificateValidationEnabled)
                httpClientHandler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;

            if (_config.IsAlwaysLogin && rootUri != null)
            {
                //Added to handle redirects clearing auth headers which result in 401...
                //https://stackoverflow.com/questions/13159589/how-to-handle-authenticatication-with-httpwebrequest-allowautoredirect
                var cache = new CredentialCache();
                cache.Add(new Uri($"http://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));
                cache.Add(new Uri($"https://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));

                httpClientHandler.Credentials = cache;
            }

            _savedHttpHandler = httpClientHandler;
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
