using NUnit.Framework;

namespace Abot.Tests.Unit
{
    [SetUpFixture]
    public class AssemblySetup
    {
        [SetUp]
        public void Setup()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
    }
}
