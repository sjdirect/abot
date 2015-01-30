using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fiddler;

namespace Abot.Tests.Integration
{
    [SetUpFixture]
    public class BeforeAfterTests
    {
        [SetUp]
        public void Before()
        {
            

            //Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
            //{
            //    if (oSession.HTTPMethodIs("CONNECT")) { oSession.oFlags["X-ReplyWithTunnel"] = "Fake for HTTPS Tunnel"; return; }
            //    if (oS.uriContains("replaceme.txt"))
            //    {
            //        oS.utilCreateResponseAndBypassServer();
            //        oS.responseBodyBytes = SessionIWantToReturn.responseBodyBytes;
            //        oS.oResponse.headers = (HTTPResponseHeaders)SessionIWantToReturn.oResponse.headers.Clone();
            //    }
            //};

            //Fiddler.FiddlerApplication.Startup(8889, FiddlerCoreStartupFlags.Default);
        }

        [TearDown]
        public void After()
        {
            //Fiddler.FiddlerApplication.Shutdown();
        }
    }
}
