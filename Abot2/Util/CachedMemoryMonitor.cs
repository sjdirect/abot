using System;
using System.Threading;
using Serilog;

namespace Abot2.Util
{
    public class CachedMemoryMonitor : IMemoryMonitor, IDisposable
    {
        IMemoryMonitor _memoryMonitor;
        ITimer _usageRefreshTimer;
        int _cachedCurrentUsageInMb;
        int _isDisposed;

        /// <param name="memoryMonitor">The monitor whose value is cached. Owned by the caller, who must dispose it.</param>
        /// <param name="cacheExpirationInSeconds">How often the cached value is refreshed</param>
        /// <param name="timeProvider">Clock and timer for the refreshes; if null, the system clock (TimeProvider.System)</param>
        public CachedMemoryMonitor(IMemoryMonitor memoryMonitor, int cacheExpirationInSeconds, TimeProvider timeProvider = null)
        {
            if (memoryMonitor == null)
                throw new ArgumentNullException("memoryMonitor");

            if (cacheExpirationInSeconds < 1)
                cacheExpirationInSeconds = 5;

            _memoryMonitor = memoryMonitor;

            UpdateCurrentUsageValue();

            var refreshPeriod = TimeSpan.FromSeconds(cacheExpirationInSeconds);
            _usageRefreshTimer = (timeProvider ?? TimeProvider.System).CreateTimer(state =>
            {
                //A callback already running when the timer is disposed can still get here
                if (Volatile.Read(ref _isDisposed) == 0)
                    UpdateCurrentUsageValue();
            }, null, refreshPeriod, refreshPeriod);
        }

        protected virtual void UpdateCurrentUsageValue()
        {
            var oldUsage = _cachedCurrentUsageInMb;
            _cachedCurrentUsageInMb = _memoryMonitor.GetCurrentUsageInMb();
            Log.Debug("Updated cached memory usage value from [{0}mb] to [{1}mb]", oldUsage, _cachedCurrentUsageInMb);
        }

        public virtual int GetCurrentUsageInMb()
        {
            return _cachedCurrentUsageInMb;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (disposing)
                _usageRefreshTimer.Dispose();
        }
    }
}
