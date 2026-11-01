using Abot2.Util;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Abot2.Core
{
    /// <summary>
    /// Rate limits or throttles on a per domain basis
    /// </summary>
    public interface IDomainRateLimiter
    {
        /// <summary>
        /// If the domain of the param has been flagged for rate limiting, it will be rate limited according to the configured minimum crawl delay
        /// </summary>
        [Obsolete("Blocks the calling thread while it waits. Use RateLimitAsync, which waits without blocking a thread and can be cancelled.")]
        void RateLimit(Uri uri);

        /// <summary>
        /// Waits until a request to the domain of the param is allowed, according to its minimum crawl delay. The wait does not block a thread.
        /// </summary>
        /// <param name="uri">The uri about to be requested</param>
        /// <param name="cancellationToken">Cancels the wait</param>
        Task RateLimitAsync(Uri uri, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Keeps a minimum delay between the starts of requests to the same domain. Lock-free: each request atomically reserves the next allowed
    /// start time of its domain and then waits for it on the TimeProvider, so concurrent requests are spaced exactly one delay apart.
    /// </summary>
    public class DomainRateLimiter : IDomainRateLimiter, IDisposable
    {
        readonly ConcurrentDictionary<string, DomainSchedule> _domains = new ConcurrentDictionary<string, DomainSchedule>();
        readonly long _defaultMinCrawlDelayInMillisecs;
        readonly TimeProvider _timeProvider;
        int _isDisposed;

        public DomainRateLimiter(long minCrawlDelayMillisecs)
            : this(minCrawlDelayMillisecs, null)
        {
        }

        /// <param name="minCrawlDelayMillisecs">Default minimum delay between requests to the same domain</param>
        /// <param name="timeProvider">Clock the delays are measured and waited on; if null, the system clock (TimeProvider.System). Pass a fake one to control time in tests.</param>
        public DomainRateLimiter(long minCrawlDelayMillisecs, TimeProvider timeProvider)
        {
            if (minCrawlDelayMillisecs < 0)
                throw new ArgumentException("minCrawlDelayMillisecs");

            _timeProvider = timeProvider ?? TimeProvider.System;

            if(minCrawlDelayMillisecs > 0)
                _defaultMinCrawlDelayInMillisecs = minCrawlDelayMillisecs + 20;//keeps the historical 20 ms margin on the default delay
        }

        [Obsolete("Blocks the calling thread while it waits. Use RateLimitAsync, which waits without blocking a thread and can be cancelled.")]
        public void RateLimit(Uri uri)
        {
            RateLimitAsync(uri).GetAwaiter().GetResult();
        }

        public Task RateLimitAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            ThrowIfDisposed();

            var schedule = GetDomainSchedule(uri, _defaultMinCrawlDelayInMillisecs);
            if (schedule == null)
                return Task.CompletedTask;

            var wait = schedule.ReserveNextStart(_timeProvider);
            if (wait <= TimeSpan.Zero)
                return Task.CompletedTask;

            Log.Debug("Rate limiting [{0}] for [{1}] milliseconds", uri.AbsoluteUri, wait.TotalMilliseconds);
            return TimeProviderWaits.Delay(_timeProvider, wait, cancellationToken);
        }

        public void AddDomain(Uri uri, long minCrawlDelayInMillisecs)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (minCrawlDelayInMillisecs < 1)
                throw new ArgumentException("minCrawlDelayInMillisecs");

            GetDomainSchedule(uri, Math.Max(minCrawlDelayInMillisecs, _defaultMinCrawlDelayInMillisecs));//just calling this method adds the new domain
        }

        public void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (minCrawlDelayInMillisecs < 1)
                throw new ArgumentException("minCrawlDelayInMillisecs");

            ThrowIfDisposed();

            var delayToUse = Math.Max(minCrawlDelayInMillisecs, _defaultMinCrawlDelayInMillisecs);
            var schedule = new DomainSchedule(delayToUse, _timeProvider);
            _domains.AddOrUpdate(uri.Authority, schedule, (key, oldValue) => schedule);
            Log.Debug("Added/updated domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, delayToUse);
        }

        public void RemoveDomain(Uri uri)
        {
            _domains.TryRemove(uri.Authority, out _);
        }

        /// <summary>
        /// The minimum delay currently used for the domain of the uri, or null if requests to it are not rate limited
        /// </summary>
        public TimeSpan? GetMinCrawlDelay(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            return _domains.TryGetValue(uri.Authority, out var schedule) ? schedule.Delay : (TimeSpan?)null;
        }

        protected virtual DomainSchedule GetDomainSchedule(Uri uri, long minCrawlDelayInMillisecs)
        {
            ThrowIfDisposed();

            if (_domains.TryGetValue(uri.Authority, out var schedule) || minCrawlDelayInMillisecs <= 0)
                return schedule;

            //GetOrAdd returns the schedule another thread added first, so a domain always has one shared schedule
            schedule = _domains.GetOrAdd(uri.Authority, authority => new DomainSchedule(minCrawlDelayInMillisecs, _timeProvider));
            Log.Debug("Rate limiting domain [{0}] with minCrawlDelayInMillisecs of [{1}] milliseconds", uri.Authority, schedule.Delay.TotalMilliseconds);
            return schedule;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Nothing to release any more: waits are plain tasks on the TimeProvider. Derived classes that own resources override this and call base.Dispose(disposing).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Interlocked.Exchange(ref _isDisposed, 1);
        }

        protected void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// The minimum delay of one domain and the earliest time its next request may start
        /// </summary>
        protected sealed class DomainSchedule
        {
            readonly long _delayInTimestampUnits;
            long _nextStartTimestamp = long.MinValue;

            public DomainSchedule(long delayInMillisecs, TimeProvider timeProvider)
            {
                Delay = TimeSpan.FromMilliseconds(delayInMillisecs);
                _delayInTimestampUnits = (long)(Delay.Ticks * (timeProvider.TimestampFrequency / (double)TimeSpan.TicksPerSecond));
            }

            public TimeSpan Delay { get; }

            /// <summary>
            /// Atomically takes the next allowed start and moves it one delay further; returns how long to wait until the taken start
            /// </summary>
            public TimeSpan ReserveNextStart(TimeProvider timeProvider)
            {
                var now = timeProvider.GetTimestamp();
                while (true)
                {
                    var next = Volatile.Read(ref _nextStartTimestamp);
                    var start = Math.Max(now, next);
                    if (Interlocked.CompareExchange(ref _nextStartTimestamp, start + _delayInTimestampUnits, next) == next)
                        return timeProvider.GetElapsedTime(now, start);
                }
            }
        }
    }
}
