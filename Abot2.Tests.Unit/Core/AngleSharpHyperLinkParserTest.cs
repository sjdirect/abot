using System;
using Abot2.Core;
using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Core
{
    [TestClass]
    public class AngleSharpHyperLinkParserTest : HyperLinkParserTest
    {
        protected override HyperLinkParser GetInstance(bool isRespectMetaRobotsNoFollowEnabled, bool isRespectAnchorRelNoFollowEnabled, Func<string, string> cleanUrlDelegate, bool isRespectUrlNamedAnchorOrHashbangEnabled, bool isHttpXRobotsTagHeaderNoFollowEnabled)
        {
            return new AngleSharpHyperlinkParser(new CrawlConfiguration
                {
                    IsRespectMetaRobotsNoFollowEnabled = isRespectMetaRobotsNoFollowEnabled,
                    IsRespectAnchorRelNoFollowEnabled = isRespectAnchorRelNoFollowEnabled,
                    IsRespectHttpXRobotsTagHeaderNoFollowEnabled = isHttpXRobotsTagHeaderNoFollowEnabled,
                    IsRespectUrlNamedAnchorOrHashbangEnabled = isRespectUrlNamedAnchorOrHashbangEnabled
                },
                cleanUrlDelegate);
        }

        [TestMethod]
        public void Constructor()
        {
            new AngleSharpHyperlinkParser();
        }
    }
}