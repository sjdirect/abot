using System;
using System.Threading;

namespace Abot.Core
{
    /// <summary>
    /// A ThreadManager implementation that will use real Threads to handle concurrency.
    /// </summary>
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