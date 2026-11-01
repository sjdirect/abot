using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;

//Each test method runs in parallel with the others; timing-sensitive classes opt out with [DoNotParallelize]
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Abot2.Tests.Integration
{
    [TestClass]
    public static class SetupAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate: Constants.LogFormatTemplate)
                .CreateLogger();
        }
    }
}
