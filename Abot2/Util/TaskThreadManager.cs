using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Abot2.Util
{

    /// <summary>
    /// A ThreadManager implementation that will use tpl Tasks to handle concurrency.
    /// </summary>
    [Obsolete("Abot no longer uses thread managers: the crawler processes pages on its own async workers, limited by CrawlConfiguration.MaxConcurrentThreads. For your own work, use Task.Run with SemaphoreSlim.WaitAsync, or Parallel.ForEachAsync on .NET 6 and later.")]
    public class TaskThreadManager : ThreadManager
    {
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly bool _ownsCancellationTokenSource;
        int _isTaskThreadManagerDisposed;

        public TaskThreadManager(int maxConcurrentTasks)
            :this(maxConcurrentTasks, null)
        {
        }

        /// <param name="maxConcurrentTasks">Max number of tasks to run at the same time</param>
        /// <param name="cancellationTokenSource">Cancelled by AbortAll() and Dispose(). Owned by the caller, who must dispose it; if null, an internal one is created and disposed with this instance.</param>
        public TaskThreadManager(int maxConcurrentTasks, CancellationTokenSource cancellationTokenSource)
            : base(maxConcurrentTasks)
        {
            _ownsCancellationTokenSource = cancellationTokenSource == null;
            _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
        }

        public override void AbortAll()
        {
            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                //Already disposed, either with this instance or by the caller that owns it, so there is nothing left to cancel
            }
            base.AbortAll();
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _isTaskThreadManagerDisposed, 1) != 0)
                return;

            //Cancels running tasks through AbortAll() before the token source is disposed
            base.Dispose(disposing);

            if (disposing && _ownsCancellationTokenSource)
                _cancellationTokenSource.Dispose();
        }

        protected override void RunActionOnDedicatedThread(Action action)
        {
            Task.Factory
                .StartNew(() => RunAction(action), _cancellationTokenSource.Token)
                .ContinueWith(HandleAggregateExceptions, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// This was added to resolve the issue described here
        /// http://stackoverflow.com/questions/7883052/a-tasks-exceptions-were-not-observed-either-by-waiting-on-the-task-or-accessi
        /// </summary>
        private void HandleAggregateExceptions(Task task)
        {
            if (task?.Exception == null) 
                return;

            var aggException = task.Exception.Flatten();
            foreach (var exception in aggException.InnerExceptions)
            {
                if(_cancellationTokenSource.IsCancellationRequested)
                    Log.Warning("CancellationRequested Aggregate Exception: {0}", exception);//If the task was cancelled then this exception is expected happen and we dont care
                else
                    Log.Error("Aggregate Exception: {0}", exception);//If the task was not cancelled then this is an error
            }
        }
    }
}
