using Abot.Core;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class CsQueryHyperLinkParserTest : HyperLinkParserTest
    {
        protected override HyperLinkParser GetInstance(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanUrlDelegate = null, bool isRespectUrlNamedAnchorOrHashbangEnabled = false)
        {
            return new CSQueryHyperlinkParser(isRespectMetaRobotsNoFollowEnabled, isRespectAnchorRelNoFollowEnabled, cleanUrlDelegate, isRespectUrlNamedAnchorOrHashbangEnabled);
        }

        [Test]
        public void Constructor()
        {
            new CSQueryHyperlinkParser();
        }
    }
}
