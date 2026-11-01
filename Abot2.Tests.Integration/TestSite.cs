using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Abot2.Tests.Integration
{
    /// <summary>
    /// Small in-process website the integration tests crawl, so no external server or proxy is needed.
    /// Mirrors the structure of the original PageGenerator test app:
    ///   /                              links to 4 generator pages and 2 "external" pages
    ///   /PageGenerator/Generate?...    links to 5x200, 1x403, 2x404, 3x500 and 4x503 pages
    ///   /HttpResponse/StatusXXX/PageN  returns status XXX with no links
    /// The "external" pages are served by this same server under the other loopback name (localhost vs 127.0.0.1),
    /// which the crawler treats as a different domain, so all traffic stays on the machine.
    /// Built on HttpListener so it runs on both .NET and .NET Framework.
    /// </summary>
    internal sealed class TestSite : IDisposable
    {
        public const int GeneratorPageCount = 4;
        public const int ExternalPageCount = 2;

        // Status code and number of links per generator page, in the same proportions as the old app
        static readonly StatusLinks[] LinksPerGeneratorPage =
        {
            new StatusLinks(200, 5), new StatusLinks(403, 1), new StatusLinks(404, 2), new StatusLinks(500, 3), new StatusLinks(503, 4)
        };

        const string GeneratorQuery = "Status200Count=5&Status403Count=1&Status404Count=2&Status500Count=3&Status503Count=4";

        static readonly Regex StatusPageRoute = new Regex(@"^/HttpResponse/Status(\d+)/Page(\d+)$", RegexOptions.IgnoreCase);
        static readonly Regex ExternalPageRoute = new Regex(@"^/External/Page(\d+)$", RegexOptions.IgnoreCase);

        readonly HttpListener _listener;
        readonly bool _holdResponses;
        readonly TaskCompletionSource<bool> _responsesReleased = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly CancellationTokenSource _stopping = new CancellationTokenSource();
        readonly Task _acceptLoop;

        TestSite(HttpListener listener, int port, bool holdResponses)
        {
            _listener = listener;
            _holdResponses = holdResponses;
            BaseUri = new Uri($"http://127.0.0.1:{port}/");
            ExternalBaseUri = new Uri($"http://localhost:{port}/");
            _acceptLoop = Task.Run(AcceptRequestsAsync);
        }

        /// <summary>Root of the site, the uri to crawl</summary>
        public Uri BaseUri { get; }

        /// <summary>Same server under the other loopback name, seen by the crawler as an external domain</summary>
        public Uri ExternalBaseUri { get; }

        /// <summary>Raised when a request arrives, before it is held or answered</summary>
        public event Action<Uri> RequestReceived;

        /// <param name="cancellationToken">Cancels starting the site</param>
        public static Task<TestSite> StartAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(false, cancellationToken);
        }

        /// <summary>
        /// Starts a site that holds every response until <see cref="ReleaseResponses"/> is called, so a test decides exactly when
        /// requests complete instead of relying on how long they take
        /// </summary>
        /// <param name="cancellationToken">Cancels starting the site</param>
        public static Task<TestSite> StartHoldingResponsesAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(true, cancellationToken);
        }

        static Task<TestSite> StartAsync(bool holdResponses, CancellationToken cancellationToken)
        {
            const int maxAttempts = 10;

            for (var attempt = 1; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var port = GetFreeTcpPort();

                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Prefixes.Add($"http://localhost:{port}/");
                try
                {
                    listener.Start();
                    return Task.FromResult(new TestSite(listener, port, holdResponses));
                }
                catch (HttpListenerException) when (attempt < maxAttempts)
                {
                    //Another process took the port between GetFreeTcpPort() releasing it and the listener binding it, try another one
                    listener.Close();
                }
            }
        }

        /// <summary>Every page a full crawl with external page crawling enabled should find, with its http status</summary>
        public List<PageResult> GetAllPages()
        {
            var pages = new List<PageResult> { new PageResult { Url = BaseUri.AbsoluteUri, HttpStatusCode = 200 } };

            for (var page = 1; page <= GeneratorPageCount; page++)
                pages.Add(new PageResult { Url = new Uri(BaseUri, GeneratorPath(page)).AbsoluteUri, HttpStatusCode = 200 });

            foreach (var links in LinksPerGeneratorPage)
            {
                for (var page = 1; page <= links.Count * GeneratorPageCount; page++)
                    pages.Add(new PageResult { Url = new Uri(BaseUri, StatusPath(links.Status, page)).AbsoluteUri, HttpStatusCode = links.Status });
            }

            for (var page = 1; page <= ExternalPageCount; page++)
                pages.Add(new PageResult { Url = new Uri(ExternalBaseUri, ExternalPath(page)).AbsoluteUri, HttpStatusCode = 200 });

            return pages;
        }

        /// <summary>
        /// Lets held responses complete, on a site started with <see cref="StartHoldingResponsesAsync"/>
        /// </summary>
        public void ReleaseResponses()
        {
            _responsesReleased.TrySetResult(true);
        }

        public void Dispose()
        {
            ReleaseResponses();
            _stopping.Cancel();
            _listener.Close();//makes the pending GetContextAsync() throw, which ends the accept loop

            try
            {
                _acceptLoop.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                //The loop only ends by the listener closing, there is nothing to report
            }

            _stopping.Dispose();
        }

        async Task AcceptRequestsAsync()
        {
            while (!_stopping.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception e) when (e is HttpListenerException || e is ObjectDisposedException || e is InvalidOperationException)
                {
                    return;//the listener was closed
                }

                _ = Task.Run(() => RespondAsync(context));
            }
        }

        async Task RespondAsync(HttpListenerContext context)
        {
            try
            {
                RequestReceived?.Invoke(context.Request.Url);

                if (_holdResponses)
                    await _responsesReleased.Task.ConfigureAwait(false);

                Route(context.Request.Url, out var status, out var html);

                var body = Encoding.UTF8.GetBytes(html);
                context.Response.StatusCode = status;
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = body.Length;
                await context.Response.OutputStream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
                context.Response.Close();
            }
            catch (Exception)
            {
                //The crawler cancelled the request or the site is shutting down
                try { context.Response.Abort(); } catch (Exception) { }
            }
        }

        void Route(Uri url, out int status, out string html)
        {
            var path = url.AbsolutePath;
            status = 200;

            if (path == "/")
            {
                var links = new List<string>();
                for (var page = 1; page <= GeneratorPageCount; page++)
                    links.Add(GeneratorPath(page));
                for (var page = 1; page <= ExternalPageCount; page++)
                    links.Add(new Uri(ExternalBaseUri, ExternalPath(page)).AbsoluteUri);

                html = HtmlDocument("Home", links);
                return;
            }

            // Page numbers are assigned per generator page, so links are the same whatever order pages are requested in
            if (string.Equals(path, "/PageGenerator/Generate", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(GetQueryValue(url, "Page"), out var generatorPage))
            {
                var links = new List<string>();
                foreach (var statusLinks in LinksPerGeneratorPage)
                {
                    for (var i = 1; i <= statusLinks.Count; i++)
                        links.Add(StatusPath(statusLinks.Status, (generatorPage - 1) * statusLinks.Count + i));
                }

                html = HtmlDocument("Generated page " + generatorPage, links);
                return;
            }

            var match = StatusPageRoute.Match(path);
            if (match.Success)
            {
                status = int.Parse(match.Groups[1].Value);
                html = HtmlDocument($"Status {status} page {match.Groups[2].Value}", new List<string>());
                return;
            }

            match = ExternalPageRoute.Match(path);
            if (match.Success)
            {
                html = HtmlDocument("External page " + match.Groups[1].Value, new List<string>());
                return;
            }

            status = 404;
            html = string.Empty;
        }

        static string GetQueryValue(Uri url, string name)
        {
            foreach (var pair in url.Query.TrimStart('?').Split('&'))
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(parts[1]);
            }

            return null;
        }

        static string GeneratorPath(int page) => $"/PageGenerator/Generate?{GeneratorQuery}&Page={page}";

        static string StatusPath(int status, int page) => $"/HttpResponse/Status{status}/Page{page}";

        static string ExternalPath(int page) => $"/External/Page{page}";

        static string HtmlDocument(string title, List<string> links)
        {
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><title>").Append(title).Append("</title></head><body><ul>");
            foreach (var link in links)
                html.Append("<li><a href=\"").Append(WebUtility.HtmlEncode(link)).Append("\">").Append(WebUtility.HtmlEncode(link)).Append("</a></li>");
            html.Append("</ul></body></html>");
            return html.ToString();
        }

        static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        sealed class StatusLinks
        {
            public StatusLinks(int status, int count)
            {
                Status = status;
                Count = count;
            }

            public int Status { get; }

            public int Count { get; }
        }
    }
}
