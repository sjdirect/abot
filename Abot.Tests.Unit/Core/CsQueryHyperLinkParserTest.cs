using Abot.Core;
using NUnit.Framework;
using System;
using Abot.Poco;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class CsQueryHyperLinkParserTest : HyperLinkParserTest
    {
        protected override HyperLinkParser GetInstance(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanUrlDelegate, bool isRespectUrlNamedAnchorOrHashbangEnabled, bool isHttpXRobotsTagHeaderNoFollowEnabled)
        {
            return new CSQueryHyperlinkParser(new CrawlConfiguration
            {
                IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled,
                IsRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled,
                IsRespectHttpXRobotsTagHeaderNoFollowEnabled = isHttpXRobotsTagHeaderNoFollowEnabled,
                IsRespectUrlNamedAnchorOrHashbangEnabled = isRespectUrlNamedAnchorOrHashbangEnabled
            },
            cleanUrlDelegate);
        }

        [Test]
        public void Constructor()
        {
            new CSQueryHyperlinkParser();
        }
    }
}
