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
        /// <param name="uri">The url to request</param>
        /// <param name="cancellationToken">Cancels the request; the returned task is then cancelled instead of reporting an error on the page</param>
        Task<CrawledPage> MakeRequestAsync(Uri uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        /// <param name="uri">The url to request</param>
        /// <param name="shouldDownloadContent">Decides, once the response headers have arrived, whether to download the content</param>
        /// <param name="cancellationToken">Cancels the request; the returned task is then cancelled instead of reporting an error on the page</param>
        Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent, CancellationToken cancellationToken = default);
    }

    public class PageRequester : IPageRequester
    {
        private readonly CrawlConfiguration _config;
        private readonly IWebContentExtractor _contentExtractor;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private readonly bool _ownsHttpClient;
        private readonly TimeProvider _timeProvider;
        private readonly IAngleSharpHtmlParserFactory _htmlParserFactory;
        private HttpClients _httpClients;
        private int _isDisposed;

        /// <param name="config">Configurable crawl values</param>
        /// <param name="contentExtractor">Extracts the content of a response. Owned by the caller, who must dispose it.</param>
        /// <param name="httpClient">Owned by the caller, who must dispose it; if null, an internal one is created on the first request and disposed with this instance.</param>
        /// <param name="timeProvider">Clock for the request and download times on the crawled pages; if null, the system clock (TimeProvider.System)</param>
        public PageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor, HttpClient httpClient = null, TimeProvider timeProvider = null)
            : this(config, contentExtractor, httpClient, timeProvider, AngleSharpHtmlParserFactory.Default)
        {
        }

        /// <param name="config">Configurable crawl values</param>
        /// <param name="contentExtractor">Extracts the content of a response. Owned by the caller, who must dispose it.</param>
        /// <param name="httpClient">Owned by the caller, who must dispose it; if null, an internal one is created on the first request and disposed with this instance.</param>
        /// <param name="timeProvider">Clock for the request and download times on the crawled pages; if null, the system clock (TimeProvider.System)</param>
        /// <param name="htmlParserFactory">Creates the parser for each crawled page's AngleSharpHtmlDocument; AngleSharpHtmlParserFactory.Default is what the other constructor uses</param>
        public PageRequester(CrawlConfiguration config, IWebContentExtractor contentExtractor, HttpClient httpClient, TimeProvider timeProvider, IAngleSharpHtmlParserFactory htmlParserFactory)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (_config.HttpRequestTimeoutInSeconds < CrawlConfiguration.NoHttpRequestTimeout)
                throw new ArgumentOutOfRangeException(nameof(config), _config.HttpRequestTimeoutInSeconds,
                    "CrawlConfiguration.HttpRequestTimeoutInSeconds must be greater than 0, 0 for HttpClient's default timeout, or -1 for no timeout.");

            _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));

#if !NET
            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;
#endif

            _ownsHttpClient = httpClient == null;
            if (httpClient != null)
                _httpClients = new HttpClients(null, httpClient);
            _timeProvider = timeProvider ?? TimeProvider.System;
            _htmlParserFactory = htmlParserFactory ?? throw new ArgumentNullException(nameof(htmlParserFactory));
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual Task<CrawledPage> MakeRequestAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            return MakeRequestAsync(uri, (x) => new CrawlDecision { Allow = true }, cancellationToken);
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent, CancellationToken cancellationToken = default)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            var httpClients = GetHttpClients(uri);

            var crawledPage = new CrawledPage(uri, _htmlParserFactory);
            HttpResponseMessage response = null;
            try
            {
                crawledPage.RequestStarted = Now();
                using (var requestMessage = BuildHttpRequestMessage(uri))
                {
                    response = await httpClients.Client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                }

                var statusCode = Convert.ToInt32(response.StatusCode);
                if (statusCode < 200 || statusCode > 399)
                    throw new HttpRequestException($"Server response was unsuccessful, returned [http {statusCode}]");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                //Cancelled by the caller, which is not an error of this page
                response?.Dispose();
                throw;
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

            crawledPage.HttpRequestMessage = response?.RequestMessage;
            crawledPage.RequestCompleted = Now();
            crawledPage.HttpResponseMessage = response;
            crawledPage.HttpClientHandler = httpClients.Handler;

            try
            {
                if (response != null)
                {
                    var shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                    if (shouldDownloadContentDecision.Allow)
                    {
                        crawledPage.DownloadContentStarted = Now();
                        crawledPage.Content = await _contentExtractor.GetContentAsync(response, cancellationToken).ConfigureAwait(false);
                        crawledPage.DownloadContentCompleted = Now();
                    }
                    else
                    {
                        Log.Debug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Debug("Error occurred finalizing requesting url [{0}] {@Exception}", uri.AbsoluteUri, e);
            }

            return crawledPage;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (disposing)
                DisposeHttpClients(Interlocked.Exchange(ref _httpClients, null));
        }

        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        //Local, like DateTime.Now; GetLocalNow().DateTime alone would be Unspecified
        private DateTime Now()
        {
            return DateTime.SpecifyKind(_timeProvider.GetLocalNow().DateTime, DateTimeKind.Local);
        }

        //Built on the first request, lock-free: if several first requests race, one client is published and the others are disposed
        private HttpClients GetHttpClients(Uri rootUri)
        {
            var httpClients = Volatile.Read(ref _httpClients);
            if (httpClients != null)
                return httpClients;

            var handler = BuildHttpClientHandler(rootUri);
            var built = new HttpClients(handler, BuildHttpClient(handler));

            var published = Interlocked.CompareExchange(ref _httpClients, built, null);
            if (published != null)
            {
                DisposeHttpClients(built);
                return published;
            }

            //Disposed while building: Dispose() may already have taken the field, so release what was just published
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                DisposeHttpClients(Interlocked.Exchange(ref _httpClients, null));
                throw new ObjectDisposedException(GetType().FullName);
            }

            return built;
        }

        private void DisposeHttpClients(HttpClients httpClients)
        {
            if (httpClients == null)
                return;

            if (_ownsHttpClient)
                httpClients.Client.Dispose();
            httpClients.Handler?.Dispose();//only ever built here, never passed in
        }

        private sealed class HttpClients
        {
            public HttpClients(HttpClientHandler handler, HttpClient client)
            {
                Handler = handler;
                Client = client;
            }

            public HttpClientHandler Handler { get; }

            public HttpClient Client { get; }
        }
        
        
        protected virtual HttpRequestMessage BuildHttpRequestMessage(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            
            request.Version = GetEquivalentHttpProtocolVersion();
            
            return request;
        }
        
        protected virtual HttpClient BuildHttpClient(HttpClientHandler clientHandler)
        {
            var httpClient = new HttpClient(clientHandler);

            httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgentString);
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            if (_config.HttpRequestTimeoutInSeconds > 0)
                httpClient.Timeout = TimeSpan.FromSeconds(_config.HttpRequestTimeoutInSeconds);
            else if (_config.HttpRequestTimeoutInSeconds == CrawlConfiguration.NoHttpRequestTimeout)
                httpClient.Timeout = Timeout.InfiniteTimeSpan;

            if (_config.IsAlwaysLogin)
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(_config.LoginUser + ":" + _config.LoginPassword));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
            }

            return httpClient;
        }

        protected virtual HttpClientHandler BuildHttpClientHandler(Uri rootUri)
        {
            if(rootUri == null)
                throw new ArgumentNullException(nameof(rootUri));

#if NETFRAMEWORK
            //HttpClientHandler.ServerCertificateCustomValidationCallback needs .NET Framework 4.7.1, WebRequestHandler has a per-handler equivalent
            var httpClientHandler = new WebRequestHandler
#else
            var httpClientHandler = new HttpClientHandler
#endif
            {
                MaxAutomaticRedirections = _config.HttpRequestMaxAutoRedirects,
                UseDefaultCredentials = _config.UseDefaultCredentials
            };

#if NET
            //ServicePointManager has no effect on HttpClient in .NET Core and later
            if (_config.HttpServicePointConnectionLimit > 0)
                httpClientHandler.MaxConnectionsPerServer = _config.HttpServicePointConnectionLimit;
#endif

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
#if NETFRAMEWORK
                httpClientHandler.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;
#else
                httpClientHandler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;
#endif

            if (_config.IsAlwaysLogin && rootUri != null)
            {
                //Added to handle redirects clearing auth headers which result in 401...
                //https://stackoverflow.com/questions/13159589/how-to-handle-authenticatication-with-httpwebrequest-allowautoredirect
                var cache = new CredentialCache();
                cache.Add(new Uri($"http://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));
                cache.Add(new Uri($"https://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));

                httpClientHandler.Credentials = cache;
            }

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
