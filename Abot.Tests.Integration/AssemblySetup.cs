using NUnit.Framework;

namespace Abot.Tests.Integration
{
    [SetUpFixture]
    public class AssemblySetup
    {
		public static bool IsWindows()
		{
			return System.Environment.OSVersion.ToString().Contains("Windows");
		}

        [SetUp]
        public void Setup()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
    }
}
