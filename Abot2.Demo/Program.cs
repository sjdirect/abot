using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;

namespace Abot2.Demo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //The demos run in parallel, so every line says which demo wrote it: [Demo] comes from the LogContext RunDemo sets,
            //or is Main outside the demos. WithProperty must come after FromLogContext, as it only adds Demo when it isn't set yet.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithThreadId()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Demo", "Main")
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss:fff zzz}] [{ThreadId}] [{Level:u3}] [{Demo}] - {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Demo starting up!");

            //Which demos run, and the uri each one uses, are set in appsettings.json.
            //appsettings.Local.json, if present, overrides it and is ignored by git, for your own settings.
            var settings = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .Build();

            //The enabled demos run at the same time, to show Abot's parts can be used from several threads at once
            var demos = new List<Task>();
            if (IsDemoEnabled(settings, "PageRequester"))
                demos.Add(RunDemo("PageRequester", () => DemoPageRequester(GetDemoUri(settings, "PageRequester"))));
            if (IsDemoEnabled(settings, "SimpleCrawler"))
                demos.Add(RunDemo("SimpleCrawler", () => DemoSimpleCrawler(GetDemoUri(settings, "SimpleCrawler"))));
            if (demos.Count == 0)
                Log.Warning("No demo is enabled: set Demos:<name>:Enabled to true in appsettings.json");

            await Task.WhenAll(demos);

            Log.Information("Demo done!");
            Console.ReadKey();
        }

        private static Task RunDemo(string demo, Func<Task> run)
        {
            return Task.Run(async () =>
            {
                using (LogContext.PushProperty("Demo", demo))
                    await run();
            });
        }

        private static bool IsDemoEnabled(IConfiguration settings, string demo)
        {
            var key = $"Demos:{demo}:Enabled";
            var value = settings[key];
            if (!bool.TryParse(value, out var enabled))
                throw new InvalidOperationException($"Set {key} in appsettings.json or appsettings.Local.json to true or false; it is [{value}].");

            return enabled;
        }

        private static Uri GetDemoUri(IConfiguration settings, string demo)
        {
            var key = $"Demos:{demo}:Uri";
            var value = settings[key];
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Set {key} in appsettings.json or appsettings.Local.json to the absolute uri the {demo} demo should use; it is [{value}].");

            return uri;
        }

        private static async Task DemoSimpleCrawler(Uri uri)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 25,
                MinCrawlDelayPerDomainMilliSeconds = 3000
            };
            using var crawler = new PoliteWebCrawler(config);

            crawler.PageCrawlCompleted += Crawler_PageCrawlCompleted;

            Log.Information("SimpleCrawler demo: crawling [{uri}]", uri);
            var crawlResult = await crawler.CrawlAsync(uri);
            Log.Information("SimpleCrawler demo done: crawled [{pages}] pages in [{elapsed}], error: [{error}]",
                crawlResult.CrawlContext.CrawledCount, crawlResult.Elapsed, crawlResult.ErrorException?.Message ?? "none");
        }

        private static void Crawler_PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {

        }

        private static async Task DemoPageRequester(Uri uri)
        {
            using var contentExtractor = new WebContentExtractor();
            using var pageRequester = new PageRequester(new CrawlConfiguration(), contentExtractor);

            Log.Information("PageRequester demo: requesting [{uri}]", uri);
            var result = await pageRequester.MakeRequestAsync(uri);
            Log.Information("PageRequester demo done: {result}", new { url = result.Uri, status = Convert.ToInt32(result.HttpResponseMessage.StatusCode) });
        }
    }
}
