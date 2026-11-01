using System;
using System.Threading;

namespace Abot2.Util
{
    /// <summary>
    /// A ThreadManager implementation that will use real Threads to handle concurrency.
    /// </summary>
    [Obsolete("Abot no longer uses thread managers: the crawler processes pages on its own async workers, limited by CrawlConfiguration.MaxConcurrentThreads. For your own work, use Task.Run with SemaphoreSlim.WaitAsync, or Parallel.ForEachAsync on .NET 6 and later.")]
    public class ManualThreadManager : ThreadManager
    {
        public ManualThreadManager(int maxThreads)
            :base(maxThreads)
        {
        }

        protected override void RunActionOnDedicatedThread(Action action)
        {
            new Thread(() => RunAction(action)).Start();
        }
    }
}