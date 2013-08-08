
namespace Abot.Poco
{
    public class CrawlDecision
    {
        public CrawlDecision()
        {
            Reason = "";
        }

        /// <summary>
        /// Whether to allow the crawl decision
        /// </summary>
        public bool Allow { get; set; }

        /// <summary>
        /// The reason the crawl decision was NOT allowed
        /// </summary>
        public string Reason { get; set; }
    }
}
