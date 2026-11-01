using System;
using System.Diagnostics;
using Serilog;

namespace Abot2.Util
{
    public interface IMemoryMonitor : IDisposable
    {
        int GetCurrentUsageInMb();
    }

    public class GcMemoryMonitor : IMemoryMonitor
    {
        public virtual int GetCurrentUsageInMb()
        {
            var timer = Stopwatch.StartNew();
            var currentUsageInMb = Convert.ToInt32(GC.GetTotalMemory(false) / (1024 * 1024));
            timer.Stop();

            Log.Debug("GC reporting [{0}mb] currently thought to be allocated, took [{1}] millisecs", currentUsageInMb, timer.ElapsedMilliseconds);

            return currentUsageInMb;       
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Nothing to release here. Derived classes that own resources override this and call base.Dispose(disposing).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
