﻿using log4net;
using System;
using System.Timers;

namespace Abot.Core
{
    public class CachedMemoryMonitor : IMemoryMonitor
    {
        static ILog _logger = LogManager.GetLogger(typeof(GcMemoryMonitor).FullName);
        IMemoryMonitor _memoryMonitor;
        Timer _usageRefreshTimer;
        int _cachedCurrentUsageInMb;

        public CachedMemoryMonitor(IMemoryMonitor memoryMonitor, int cacheExpirationInSeconds)
        {
            if (memoryMonitor == null)
                throw new ArgumentNullException("memoryMonitor");

            if (cacheExpirationInSeconds < 1)
                cacheExpirationInSeconds = 5;

            _memoryMonitor = memoryMonitor;

            UpdateCurrentUsageValue();

            _usageRefreshTimer = new Timer(cacheExpirationInSeconds * 1000);
            _usageRefreshTimer.Elapsed += (sender, e) => UpdateCurrentUsageValue();
            _usageRefreshTimer.Start();
        }

        protected virtual void UpdateCurrentUsageValue()
        {
            int oldUsage = _cachedCurrentUsageInMb;
            _cachedCurrentUsageInMb = _memoryMonitor.GetCurrentUsageInMb();
            _logger.DebugFormat("Updated cached memory usage value from [{0}mb] to [{1}mb]", oldUsage, _cachedCurrentUsageInMb);
        }

        public virtual int GetCurrentUsageInMb()
        {
            return _cachedCurrentUsageInMb;
        }

        public void Dispose()
        {
            _usageRefreshTimer.Stop();
            _usageRefreshTimer.Dispose();
			_memoryMonitor.Dispose();
        }
    }
}
