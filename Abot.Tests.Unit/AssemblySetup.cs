using log4net.Config;
using NUnit.Framework;

namespace Abot.Tests.Unit
{
    [SetUpFixture]
    public class AssemblySetup
    {
        [SetUp]
        public void Setup()
        {
            XmlConfigurator.Configure();

            FiddlerProxyUtil.StartAutoRespond(@"..\..\..\TestResponses.saz");
        }

        [TearDown]
        public void After()
        {
            FiddlerProxyUtil.StopAutoResponding();
        }
    }
}
