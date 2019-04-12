using System;
using System.Net.Http;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Poco;
using Serilog;

namespace Abot2.Demo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Demo starting up!");

            var pageRequester =
                new PageRequester(new CrawlConfiguration(), new WebContentExtractor(), new HttpClient());

            var result = await pageRequester.MakeRequestAsync(new Uri("http://google.com"));
            Log.Logger.Information("{result}", new { url = result.Uri, status = Convert.ToInt32(result.HttpResponseMessage.StatusCode)});
            
            Console.ReadKey();
        }
    }
}
