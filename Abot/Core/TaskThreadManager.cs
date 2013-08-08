using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abot.Core
{
    public class TaskThreadManager : IThreadManager
    {
        static ILog _logger = LogManager.GetLogger(typeof(TaskThreadManager));

        /// <summary>
        /// Maximum number of concurrently running tasks allowed.
        /// Note that 1 task does not equal 1 thread.
        /// </summary>
        public int MaxThreads
        {
            get;
            private set;
        }

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private object _freeTaskLock = new object();
        private int _freeTaskCount;

        /// <summary>
        /// Create a new thread manager that will use Tasks to handle concurrency.
        /// </summary>
        /// <param name="maxConcurrentTasks">The maximum number of concurrently running tasks allowed</param>
        public TaskThreadManager(int maxConcurrentTasks)
        {
            if (maxConcurrentTasks <= 0)
                throw new ArgumentException("Max concurrent tasks must be greater than 0.", "maxConcurrentTasks");

            _freeTaskCount = maxConcurrentTasks;
            MaxThreads = maxConcurrentTasks;
        }

        /// <summary>
        /// Wait for a task to become available and then perform the specified action.
        /// </summary>
        public void DoWork(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (_cancellationTokenSource.IsCancellationRequested)
                throw new InvalidOperationException("Cannot call DoWork() after AbortAll() or Dispose() have been called.");


            //Spin until we can create a new task without exceeding the limit
            while (true)
            {
                if (_freeTaskCount > 0)
                {
                    lock (_freeTaskLock)
                    {
                        if (_freeTaskCount > 0)
                        {
                            _freeTaskCount--;
                            break;
                        }
                    }
                }

                //Yield so that we don't starve other threads
                Thread.Sleep(0);
            }

            _logger.DebugFormat("Starting up a task on thread id {0}.", Thread.CurrentThread.ManagedThreadId);

            Task workTask = new Task(action, _cancellationTokenSource.Token);
            workTask.ContinueWith(ReleaseTask);
            workTask.Start();
        }

        /// <summary>
        /// Whether there are any tasks currently executing
        /// </summary>
        public bool HasRunningThreads()
        {
            return _freeTaskCount < MaxThreads;
        }

        /// <summary>
        /// Stop all running tasks.
        /// </summary>
        public void AbortAll()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private void ReleaseTask(Task t)
        {
            lock (_freeTaskLock)
            {
                _freeTaskCount++;
            }

            _logger.Debug("Task complete");
        }
    }
}
