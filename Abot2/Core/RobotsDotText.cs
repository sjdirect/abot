using Robots;
using System;

namespace Abot2.Core
{
    public interface IRobotsDotText
    {
        /// <summary>
        /// Gets the number of seconds to delay between internal page crawls. Returns 0 by default.
        /// </summary>
        int GetCrawlDelay(string userAgentString);

        /// <summary>
        /// Whether the spider is "allowed" to crawl the param link
        /// </summary>
        bool IsUrlAllowed(string url, string userAgentString);

        /// <summary>
        /// Whether the user agent is "allowed" to crawl the root url
        /// </summary>
        bool IsUserAgentAllowed(string userAgentString);

        /// <summary>
        /// Instance of robot.txt object
        /// </summary>
        IRobots Robots { get; }
    }

    public class RobotsDotText : IRobotsDotText
    {
        private Robots.Robots _robotsDotTextUtil;
        private readonly Uri _rootUri;

        public RobotsDotText(Uri rootUri, string content)
        {
            _rootUri = rootUri ?? throw new ArgumentNullException(nameof(rootUri));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            Load(rootUri, content);           
        }

        public int GetCrawlDelay(string userAgentString)
        {
            return _robotsDotTextUtil.GetCrawlDelay(UserAgentOrAll(userAgentString));
        }

        public bool IsUrlAllowed(string url, string userAgentString)
        {
            if (!_rootUri.IsBaseOf(new Uri(url)))
                return true;

            return _robotsDotTextUtil.Allowed(url, UserAgentOrAll(userAgentString));
        }

        public bool IsUserAgentAllowed(string userAgentString)
        {
            return _robotsDotTextUtil.Allowed(_rootUri.AbsoluteUri, UserAgentOrAll(userAgentString));
        }

        public IRobots Robots => _robotsDotTextUtil;

        //A null user agent means every agent ("*"), as in 2.x. NRobotsCore 1.x treated null that way itself; newer versions throw.
        private static string UserAgentOrAll(string userAgentString) => userAgentString ?? "*";

        private void Load(Uri rootUri, string content)
        {
            _robotsDotTextUtil = new Robots.Robots();
            _robotsDotTextUtil.LoadContent(content, rootUri.AbsoluteUri);
        }
    }
}
