using Abot.Core;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class HapHyperLinkParserTest : HyperLinkParserTest
    {
        protected override HyperLinkParser GetInstance(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanUrlDelegate = null)
        {
            return new HapHyperLinkParser(isRespectMetaRobotsNoFollowEnabled, isRespectAnchorRelNoFollowEnabled, cleanUrlDelegate);
        }

        [Test]
        public void Constructor()
        {
            new HapHyperLinkParser();
        }

    }
}
