using log4net;
using System;
using System.Diagnostics;

namespace Abot.Util
{
    public interface IMemoryMonitor : IDisposable
    {
        int GetCurrentUsageInMb();
    }

    [Serializable]
    public class GcMemoryMonitor : IMemoryMonitor
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");

        public virtual int GetCurrentUsageInMb()
        {
            Stopwatch timer = Stopwatch.StartNew();
            int currentUsageInMb = Convert.ToInt32(GC.GetTotalMemory(false) / (1024 * 1024));
            timer.Stop();

            _logger.DebugFormat("GC reporting [{0}mb] currently thought to be allocated, took [{1}] millisecs", currentUsageInMb, timer.ElapsedMilliseconds);

            return currentUsageInMb;       
        }

        public void Dispose()
        {
            //do nothing
        }
    }
}
