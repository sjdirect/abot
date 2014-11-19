using log4net;
using RobotsTxt;
using System;

namespace Abot.Core
{
    public interface IRobotsDotText
    {
        /// <summary>
        /// Gets the number of seconds to delay between internal page crawls. Returns 0 by default.
        /// </summary>
        long GetCrawlDelay(string userAgentString);

        /// <summary>
        /// Whether the spider is "allowed" to crawl the param link
        /// </summary>
        bool IsUrlAllowed(string url, string userAgentString);

        /// <summary>
        /// Whether the user agent is "allowed" to crawl the root url
        /// </summary>
        bool IsUserAgentAllowed(string userAgentString);
    }

    public class RobotsDotText : IRobotsDotText
    {
        !!!!Waiting on https://code.google.com/p/robotstxt/issues/detail?id=6
        ILog _logger = LogManager.GetLogger("AbotLogger");
        Robots _robotsDotTextUtil = null;
        Uri _rootUri = null;
        bool _isAnyPathDisallowed;

        public RobotsDotText(Uri rootUri, string content)
        {
            if (rootUri == null)
                throw new ArgumentNullException("rootUri");

            if (content == null)
                throw new ArgumentNullException("content");

            _rootUri = rootUri;
            _robotsDotTextUtil = Robots.Load(content);
            _isAnyPathDisallowed = _robotsDotTextUtil.IsAnyPathDisallowed;
        }

        public long GetCrawlDelay(string userAgentString)
        {
            return _robotsDotTextUtil.CrawlDelay(userAgentString) / 1000;
        }

        public bool IsUrlAllowed(string url, string userAgentString)
        {
            if (!_rootUri.IsBaseOf(new Uri(url)))
                return true;

            bool isAllowed = false;
            try
            {
                isAllowed = _robotsDotTextUtil.IsPathAllowed(userAgentString, url);
            }
            catch(ArgumentException)
            {
                isAllowed = true;
            }

            return isAllowed;
        }

        public bool IsUserAgentAllowed(string userAgentString)
        {
            bool isAllowed = false;
            try
            {
                isAllowed = _robotsDotTextUtil.IsPathAllowed(userAgentString, _rootUri.AbsoluteUri);
            }
            catch (ArgumentException)
            {
                isAllowed = true;
            }
            return isAllowed;
        }
    }
}
