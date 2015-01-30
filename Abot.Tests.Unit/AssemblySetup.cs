using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fiddler;
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

            //https://groups.google.com/forum/#!topic/httpfiddler/mu9MrvYblJ0
            if (!FiddlerApplication.oTranscoders.ImportTranscoders(Assembly.GetExecutingAssembly()))
            {
                Console.WriteLine("This assembly was not compiled with a SAZ-exporter");
            }
            List<Session> importedSessions = new List<Session>();
            ImportSessions(importedSessions);
            Fiddler.FiddlerApplication.BeforeRequest += delegate(Fiddler.Session oS)
            {
                //if (oSession.HTTPMethodIs("CONNECT")) { oSession.oFlags["X-ReplyWithTunnel"] = "Fake for HTTPS Tunnel"; return; }
                if (oS.uriContains("replaceme.txt"))
                {
                    oS.utilCreateResponseAndBypassServer();
                    //oS.responseBodyBytes = SessionIWantToReturn.responseBodyBytes;
                    //oS.oResponse.headers = (HTTPResponseHeaders)SessionIWantToReturn.oResponse.headers.Clone();
                }
            };
            
            //FiddlerApplication.BeforeRequest += delegate(Session session)
            //{
            //    if (session.uriContains(@"http://www.website.com/request.php"))
            //    {
            //        session.bBufferResponse = true;
            //    }
            //};

            //FiddlerApplication.BeforeResponse += delegate(Session session)
            //{
            //    session.utilDecodeResponse();
            //    session.LoadResponseFromFile("website.txt");
            //};

            //Fiddler.FiddlerApplication.Startup(8889, FiddlerCoreStartupFlags.Default);
            Fiddler.FiddlerApplication.Startup(8889, false, false);
        }

        [TearDown]
        public void After()
        {
            ////FiddlerApplication.AfterSessionComplete -= FiddlerApplication_AfterSessionComplete;

            if (FiddlerApplication.IsStarted())
                FiddlerApplication.Shutdown();
        }

        //private List<Session> GetSessions()
        //{
        //    TranscoderTuple oImporter = FiddlerApplication.oTranscoders.GetImporter("SAZ");
        //    if (null != oImporter)
        //    {
        //        Dictionary<string, object> dictOptions = new Dictionary<string, object>
        //        {
        //            {"Filename", @"C:\WorkRoot\abotMaster\TestResponses.saz"}
        //        };
        //        //Path.Combine(@"..\..\..\TestResponses.saz"));

        //        Session[] oLoaded = FiddlerApplication.DoImport("SAZ", false, dictOptions, null);

        //        if ((oLoaded != null) && (oLoaded.Length > 0))
        //        {
        //            Console.WriteLine("Loaded: " + oLoaded.Length + " sessions.");
        //            return oLoaded.ToList();
        //        }
        //    }

        //    return new List<Session>();
        //}

        private static void ImportSessions(List<Fiddler.Session> oAllSessions)
        {
            TranscoderTuple oImporter = FiddlerApplication.oTranscoders.GetImporter("SAZ");
            if (null != oImporter)
            {
                Dictionary<string, object> dictOptions = new Dictionary<string, object>();
                dictOptions.Add("Filename", @"C:\WorkRoot\abotMaster\TestResponses.saz");

                Session[] oLoaded = FiddlerApplication.DoImport("SAZ", false, dictOptions, null);

                if ((oLoaded != null) && (oLoaded.Length > 0))
                {
                    oAllSessions.AddRange(oLoaded);
                    Console.WriteLine("Loaded: " + oLoaded.Length + " sessions.");
                }
            }
        }
    }
}
