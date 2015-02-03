using Abot.Util;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Abot.Core
{
    /// <summary>
    /// Rate limits or throttles on a per domain basis
    /// </summary>
    public interface IDomainRateLimiter
    {
        /// <summary>
        /// If the domain of the param has been flagged for rate limiting, it will be rate limited according to the configured minimum crawl delay
        /// </summary>
        void RateLimit(Uri uri);

        /// <summary>
        /// Add a domain entry so that domain may be rate limited according the the param minumum crawl delay
        /// </summary>
        void AddDomain(Uri uri, long minCrawlDelayInMillisecs);
    }

    [Serializable]
    public class DomainRateLimiter : IDomainRateLimiter
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        ConcurrentDictionary<string, IRateLimiter> _rateLimiterLookup = new ConcurrentDictionary<string, IRateLimiter>();
        long _defaultMinCrawlDelayInMillisecs;

        public DomainRateLimiter(long minCrawlDelayMillisecs)
        {
            if (minCrawlDelayMillisecs < 0)
                throw new ArgumentException("minCrawlDelayMillisecs");

            _defaultMinCrawlDelayInMillisecs = minCrawlDelayMillisecs;
        }

        public void RateLimit(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            IRateLimiter rateLimiter = GetRateLimter(uri, _defaultMinCrawlDelayInMillisecs);
            if (rateLimiter == null)
                return;

            Stopwatch timer = Stopwatch.StartNew();
            rateLimiter.WaitToProceed();
            timer.Stop();

            if(timer.ElapsedMilliseconds > 10)
                _logger.DebugFormat("Rate limited [{0}] [{1}] milliseconds", uri.AbsoluteUri, timer.ElapsedMilliseconds);
        }

        public void AddDomain(Uri uri, long minCrawlDelayInMillisecs)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (minCrawlDelayInMillisecs < 1)
                throw new ArgumentException("minCrawlDelayInMillisecs");

            long millThatIsGreater = minCrawlDelayInMillisecs > _defaultMinCrawlDelayInMillisecs ? minCrawlDelayInMillisecs : _defaultMinCrawlDelayInMillisecs;
            GetRateLimter(uri, millThatIsGreater);//just calling this method adds the new domain
        }

        private IRateLimiter GetRateLimter(Uri uri, long minCrawlDelayInMillisecs)
        {
            IRateLimiter rateLimiter;
            _rateLimiterLookup.TryGetValue(uri.Authority, out rateLimiter);

            if (rateLimiter == null && minCrawlDelayInMillisecs > 0)
            {
                rateLimiter = new RateLimiter(1, TimeSpan.FromMilliseconds(minCrawlDelayInMillisecs));

                if (_rateLimiterLookup.TryAdd(uri.Authority, rateLimiter))
                    _logger.DebugFormat("Added new domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, minCrawlDelayInMillisecs);
                else
                    _logger.WarnFormat("Unable to add new domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, minCrawlDelayInMillisecs);
            }

            return rateLimiter;
        }
    }
}
