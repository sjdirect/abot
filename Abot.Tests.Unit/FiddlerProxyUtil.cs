using System.Linq;
using Fiddler;

namespace Abot.Tests.Unit
{
    public class FiddlerProxyUtil
    {
        public static void StartAutoRespond(string p)
        {
            var importedSessions = SazImporter.ReadSessionArchive(@"..\..\..\TestResponses.saz").ToList();
            FiddlerApplication.BeforeRequest += delegate(Session oS)
            {
                //TODO add dictionary for lookup
                var matchedSession = importedSessions.FirstOrDefault(s => s.fullUrl == oS.fullUrl);
                if (matchedSession != null)
                {
                    oS.utilCreateResponseAndBypassServer();
                    oS.responseBodyBytes = matchedSession.responseBodyBytes;
                    oS.oResponse.headers = (HTTPResponseHeaders)matchedSession.oResponse.headers.Clone();
                }
            };

            FiddlerApplication.Startup(8889, false, false);

        }

        public static void StopAutoResponding()
        {
            if (FiddlerApplication.IsStarted())
                FiddlerApplication.Shutdown();
        }
    }
}
