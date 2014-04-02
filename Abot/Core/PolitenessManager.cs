using Abot.Poco;
using log4net;

namespace Abot.Core
{
    public class PolitenessManager
    {
        private static ILog _logger = LogManager.GetLogger(typeof(PolitenessManager).FullName);
        protected IRobotsDotText _robotsDotText;

        public PolitenessManager(CrawlConfiguration crawlConfiguration, ImplementationContainer implementationContainer)
            :base(crawlConfiguration, implementationContainer)
        {
            int robotsDotTextCrawlDelayInSecs = 0;
            int robotsDotTextCrawlDelayInMillisecs = 0;

            int maxRobotsDotTextCrawlDelayInSeconds = _crawlContext.CrawlConfiguration.MaxRobotsDotTextCrawlDelayInSeconds;
            int maxRobotsDotTextCrawlDelayInMilliSecs = maxRobotsDotTextCrawlDelayInSeconds * 1000;

            //Load robots.txt
            if (CrawlContext.CrawlConfiguration.IsRespectRobotsDotTextEnabled)
            {
                _robotsDotText = _robotsDotTextFinder.Find(uri);

                if (_robotsDotText != null)
                {
                    robotsDotTextCrawlDelayInSecs = _robotsDotText.GetCrawlDelay(_crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);
                    robotsDotTextCrawlDelayInMillisecs = robotsDotTextCrawlDelayInSecs * 1000;
                }
            }

            //Use whichever value is greater between the actual crawl delay value found, the max allowed crawl delay value or the minimum crawl delay required for every domain
            if (robotsDotTextCrawlDelayInSecs > 0 && robotsDotTextCrawlDelayInMillisecs > _crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds)
            {
                if (robotsDotTextCrawlDelayInSecs > maxRobotsDotTextCrawlDelayInSeconds)
                {
                    _logger.WarnFormat("[{0}] robot.txt file directive [Crawl-delay: {1}] is above the value set in the config value MaxRobotsDotTextCrawlDelay, will use MaxRobotsDotTextCrawlDelay value instead.", uri, robotsDotTextCrawlDelayInSecs);

                    robotsDotTextCrawlDelayInSecs = maxRobotsDotTextCrawlDelayInSeconds;
                    robotsDotTextCrawlDelayInMillisecs = robotsDotTextCrawlDelayInSecs * 1000;
                }

                _logger.WarnFormat("[{0}] robot.txt file directive [Crawl-delay: {1}] will be respected.", uri, robotsDotTextCrawlDelayInSecs);
                _domainRateLimiter.AddDomain(uri, robotsDotTextCrawlDelayInMillisecs);
            }

            if (robotsDotTextCrawlDelayInSecs > 0 || _crawlContext.CrawlConfiguration.MinCrawlDelayPerDomainMilliSeconds > 0)
                PageCrawlStarting += (s, e) => _domainRateLimiter.RateLimit(e.PageToCrawl.Uri);

            return base.Crawl(uri, cancellationTokenSource);
        }

        protected bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            bool allowedByRobots = true;
            if (_robotsDotText != null)
                allowedByRobots = _robotsDotText.IsUrlAllowed(pageToCrawl.Uri.AbsoluteUri, _crawlContext.CrawlConfiguration.RobotsDotTextUserAgentString);

            if (!allowedByRobots)
            {
                string message = string.Format("Page [{0}] not crawled, [Disallowed by robots.txt file], set IsRespectRobotsDotText=false in config file if you would like to ignore robots.txt files.", pageToCrawl.Uri.AbsoluteUri);
                _logger.DebugFormat(message);

                FirePageCrawlDisallowedEventAsync(pageToCrawl, message);
                FirePageCrawlDisallowedEvent(pageToCrawl, message);

                return false;
            }

            return allowedByRobots && base.ShouldCrawlPage(pageToCrawl);
        }
    }
}
