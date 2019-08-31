using System;
using System.Net;
using System.Threading.Tasks;
using Serilog;

namespace Abot2.Core
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
        Task<IRobotsDotText> FindAsync(Uri rootUri);
    }

    public class RobotsDotTextFinder : IRobotsDotTextFinder
    {
        IPageRequester _pageRequester;

        public RobotsDotTextFinder(IPageRequester pageRequester)
        {
            _pageRequester = pageRequester ?? throw new ArgumentNullException(nameof(pageRequester));
        }

        public async Task<IRobotsDotText> FindAsync(Uri rootUri)
        {
            if (rootUri == null)
                throw new ArgumentNullException(nameof(rootUri));

            var robotsUri = new Uri(rootUri, "/robots.txt");
            var page = await _pageRequester.MakeRequestAsync(robotsUri).ConfigureAwait(false);
            if (page == null || 
                page.HttpRequestException != null || 
                page.HttpResponseMessage == null || 
                page.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                Log.Debug("Did not find robots.txt file at [{0}]", robotsUri);
                return null;
            }

            Log.Debug("Found robots.txt file at [{0}]", robotsUri);

            return new RobotsDotText(rootUri, page.Content.Text);
        }
    }
}
