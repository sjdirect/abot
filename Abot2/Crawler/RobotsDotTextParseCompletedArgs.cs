using Abot2.Poco;
using Robots;

namespace Abot2.Crawler
{
    /// <summary>
    /// Class which hold robot txt data after successful parsing
    /// </summary>
    public class RobotsDotTextParseCompletedArgs : CrawlArgs
    {
        /// <summary>
        /// robots.txt object
        /// </summary>
        public IRobots Robots { get; set; }

        /// <summary>
        /// Contructor to be used to create an object which will path arugments when robots txt is parsed
        /// </summary>
        public RobotsDotTextParseCompletedArgs(CrawlContext crawlContext, IRobots robots) : base(crawlContext)
        {
            Robots = robots;
        }
    }
}
