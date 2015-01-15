using Abot.Poco;
using log4net;
using System;
using System.Net;

namespace Abot.Core
{
    /// <summary>
    /// Finds and builds the robots.txt file abstraction
    /// </summary>
    public interface IRobotsDotTextFinder
    {
        /// <summary>
        /// Finds the robots.txt file using the rootUri. 
        /// If rootUri is http://yahoo.com, it will look for robots at http://yahoo.com/robots.txt.
        /// If rootUri is http://music.yahoo.com, it will look for robots at http://music.yahoo.com/robots.txt
        /// </summary>
        /// <param name="rootUri">The root domain</param>
        /// <returns>Object representing the robots.txt file or returns null</returns>
        IRobotsDotText Find(Uri rootUri);
    }

    [Serializable]
    public class RobotsDotTextFinder : IRobotsDotTextFinder
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        IPageRequester _pageRequester;

        public RobotsDotTextFinder(IPageRequester pageRequester)
        {
            if (pageRequester == null)
                throw new ArgumentNullException("pageRequester");

            _pageRequester = pageRequester;
        }

        public IRobotsDotText Find(Uri rootUri)
        {
            if (rootUri == null)
                throw new ArgumentNullException("rootUri");

            Uri robotsUri = new Uri(rootUri, "/robots.txt");
            CrawledPage page = _pageRequester.MakeRequest(robotsUri);
            if (page == null || page.WebException != null || page.HttpWebResponse == null || page.HttpWebResponse.StatusCode != HttpStatusCode.OK)
            {
                _logger.DebugFormat("Did not find robots.txt file at [{0}]", robotsUri);
                return null;
            }

            _logger.DebugFormat("Found robots.txt file at [{0}]", robotsUri);
            return new RobotsDotText(rootUri, page.Content.Text);
        }
    }
}
