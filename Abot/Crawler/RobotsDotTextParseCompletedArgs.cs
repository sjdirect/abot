using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Abot.Poco;
using Robots;

namespace Abot.Crawler
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
        /// <param name="crawlContext"></param>
        /// <param name="robots"></param>
        public RobotsDotTextParseCompletedArgs(CrawlContext crawlContext, IRobots robots) : base(crawlContext)
        {
            Robots = robots;
        }
    }
}
