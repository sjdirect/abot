using Abot.Core;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class HapHyperLinkParserTest : HyperLinkParserTest
    {
        protected override HyperLinkParser GetInstance()
        {
            return new HapHyperLinkParser();
        }
    }
}
