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

        /// <summary>
        /// Add/Update a domain entry so that domain may be rate limited according the the param minumum crawl delay
        /// </summary>
        void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs);

        /// <summary>
        /// Remove a domain entry so that it will no longer be rate limited
        /// </summary>
        void RemoveDomain(Uri uri);
    }

    [Serializable]
    public class DomainRateLimiter : IDomainRateLimiter
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");
        protected ConcurrentDictionary<string, IRateLimiter> _rateLimiterLookup = new ConcurrentDictionary<string, IRateLimiter>();
        long _defaultMinCrawlDelayInMillisecs;

        public DomainRateLimiter(long minCrawlDelayMillisecs)
        {
            if (minCrawlDelayMillisecs < 0)
                throw new ArgumentException("minCrawlDelayMillisecs");

            if(minCrawlDelayMillisecs > 0)
                _defaultMinCrawlDelayInMillisecs = minCrawlDelayMillisecs + 20;//IRateLimiter is always a little under so adding a little more time
        }

        public void RateLimit(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            IRateLimiter rateLimiter = GetRateLimiter(uri, _defaultMinCrawlDelayInMillisecs);
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

            GetRateLimiter(uri, Math.Max(minCrawlDelayInMillisecs, _defaultMinCrawlDelayInMillisecs));//just calling this method adds the new domain
        }

        public void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (minCrawlDelayInMillisecs < 1)
                throw new ArgumentException("minCrawlDelayInMillisecs");

            var delayToUse = Math.Max(minCrawlDelayInMillisecs, _defaultMinCrawlDelayInMillisecs);
            if (delayToUse > 0)
            {
                var rateLimiter = new RateLimiter(1, TimeSpan.FromMilliseconds(delayToUse));

                _rateLimiterLookup.AddOrUpdate(uri.Authority, rateLimiter, (key, oldValue) => rateLimiter);
                _logger.DebugFormat("Added/updated domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, delayToUse);
            }
        }

        public void RemoveDomain(Uri uri)
        {
            IRateLimiter rateLimiter;
            _rateLimiterLookup.TryRemove(uri.Authority, out rateLimiter);
        }

        protected virtual IRateLimiter GetRateLimiter(Uri uri, long minCrawlDelayInMillisecs)
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
