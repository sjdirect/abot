using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;

namespace Abot2.Tests.Integration
{
    [TestClass]
    public class SetupAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                //.WriteTo.Sink(new RollingFileSink(
                //        @"C:\logs",
                //        new JsonFormatter(renderMessage: true))
                .CreateLogger();
            //TODO This no longer works
            //var pathToFiddlerResponseArchive = Path.Combine(Directory.GetCurrentDirectory(), "TestResponses.saz");
            //if (!File.Exists(pathToFiddlerResponseArchive))
            //    throw new InvalidOperationException("Cannot find fiddler response archive needed to run tests at " + pathToFiddlerResponseArchive);

            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //var enc1252 = Encoding.GetEncoding(1252);

            //FiddlerProxyUtil.StartAutoRespond(pathToFiddlerResponseArchive);
            //Console.WriteLine("Started FiddlerCore to autorespond with pre recorded http responses.");

        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            //FiddlerProxyUtil.StopAutoResponding();
            //Console.WriteLine("Stopped FiddlerCore");
        }
    }
}
