﻿using log4net;
using Robots;
using System;

namespace Abot.Core
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
    }

    [Serializable]
    public class RobotsDotText : IRobotsDotText
    {
        ILog _logger = LogManager.GetLogger("AbotLogger");
        IRobots _robotsDotTextUtil = null;
        Uri _rootUri = null;

        public RobotsDotText(Uri rootUri, string content)
        {
            if (rootUri == null)
                throw new ArgumentNullException("rootUri");

            if (content == null)
                throw new ArgumentNullException("content");

            _rootUri = rootUri;
            Load(rootUri, content);           
        }

        public int GetCrawlDelay(string userAgentString)
        {
            return _robotsDotTextUtil.GetCrawlDelay(userAgentString);
        }

        public bool IsUrlAllowed(string url, string userAgentString)
        {
            if (!_rootUri.IsBaseOf(new Uri(url)))
                return true;

            return _robotsDotTextUtil.Allowed(url, userAgentString);
        }

        public bool IsUserAgentAllowed(string userAgentString)
        {
            return _robotsDotTextUtil.Allowed(_rootUri.AbsoluteUri, userAgentString);
        }

        private void Load(Uri rootUri, string content)
        {
            _robotsDotTextUtil = new Robots.Robots();
            _robotsDotTextUtil.LoadContent(content, rootUri.AbsoluteUri);
        }
    }
}
