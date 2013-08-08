using Abot.Core;
using Abot.Poco;
using NUnit.Framework;
using System;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class CsQueryHyperLinkParserTest : HyperLinkParserTest
    {
        protected override HyperLinkParser GetInstance()
        {
            return new CSQueryHyperlinkParser();
        }
    }
}
