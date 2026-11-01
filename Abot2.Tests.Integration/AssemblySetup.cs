using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;

//Each test method runs in parallel with the others; timing-sensitive classes opt out with [DoNotParallelize]
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Abot2.Tests.Integration
{
    [TestClass]
    public static class SetupAssemblyInitializer
    {
        /// <summary>Test site that responds immediately</summary>
        internal static TestSite Site { get; private set; }

        [AssemblyInitialize]
        public static async Task AssemblyInit(TestContext context)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate: Constants.LogFormatTemplate)
                .CreateLogger();

            Site = await TestSite.StartAsync(context.CancellationToken);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Site?.Dispose();
        }
    }
}
