using System;
using System.IO;
using Commoner.Core.Testing;
using log4net.Config;
using NUnit.Framework;

namespace Abot.Tests.Integration
{
    [SetUpFixture]
    public class AssemblySetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            XmlConfigurator.Configure();

            string pathToFiddlerResponseArchive = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\", "TestResponses.saz"));
            if (!File.Exists(pathToFiddlerResponseArchive))
                throw new InvalidOperationException("Cannot find fiddler response archive needed to run tests at " + pathToFiddlerResponseArchive);

            FiddlerProxyUtil.StartAutoRespond(pathToFiddlerResponseArchive);
            Console.WriteLine("Started FiddlerCore to autorespond with pre recorded http responses.");
        }

        [OneTimeTearDown]
        public void After()
        {
            FiddlerProxyUtil.StopAutoResponding();
            Console.WriteLine("Stopped FiddlerCore");
        }
    }
}
