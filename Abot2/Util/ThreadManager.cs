using System;
using System.Threading;
using Serilog;

namespace Abot2.Util
{
    /// <summary>
    /// Handles the multithreading implementation details
    /// </summary>
    [Obsolete("Abot no longer uses thread managers: the crawler processes pages on its own async workers, limited by CrawlConfiguration.MaxConcurrentThreads. For your own work, use Task.Run with SemaphoreSlim.WaitAsync, or Parallel.ForEachAsync on .NET 6 and later.")]
    public interface IThreadManager : IDisposable
    {
        /// <summary>
        /// Max number of threads to use.
        /// </summary>
        int MaxThreads { get; set; }

        /// <summary>
        /// Will perform the action in parallel on a separate thread
        /// </summary>
        void DoWork(Action action);

        /// <summary>
        /// Whether there are running threads
        /// </summary>
        bool HasRunningThreads();

        /// <summary>
        /// Abort all running threads
        /// </summary>
        void AbortAll();
    }

    [Obsolete("Abot no longer uses thread managers: the crawler processes pages on its own async workers, limited by CrawlConfiguration.MaxConcurrentThreads. For your own work, use Task.Run with SemaphoreSlim.WaitAsync, or Parallel.ForEachAsync on .NET 6 and later.")]
    public abstract class ThreadManager : IThreadManager
    {
        protected bool _abortAllCalled = false;
        protected int _numberOfRunningThreads = 0;
        protected ManualResetEvent _resetEvent = new ManualResetEvent(true);
        protected object _locker = new object();
        protected volatile bool _isDisposed = false;

        protected ThreadManager(int maxThreads)
        {
            if ((maxThreads > 100) || (maxThreads < 1))
                throw new ArgumentException("MaxThreads must be from 1 to 100");

            MaxThreads = maxThreads;
        }

        /// <summary>
        /// Max number of threads to use
        /// </summary>
        public int MaxThreads
        {
            get;
            set;
        }

        /// <summary>
        /// Will perform the action in parallel on a separate thread
        /// </summary>
        public virtual void DoWork(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (_abortAllCalled)
                throw new InvalidOperationException("Cannot call DoWork() after AbortAll() has been called.");

            if (!_isDisposed && MaxThreads > 1)
            {
                _resetEvent.WaitOne();
                lock (_locker)
                {
                    //Dispose() releases callers waiting above, they must not start work
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);

                    _numberOfRunningThreads++;
                    if (_numberOfRunningThreads >= MaxThreads)
                        _resetEvent.Reset();

                    Log.Debug("Starting another thread, increasing running threads to [{0}].", _numberOfRunningThreads);
                }
                RunActionOnDedicatedThread(action);
            }
            else
            {
                RunAction(action, false);
            }
        }

        public virtual void AbortAll()
        {
            _abortAllCalled = true;
            lock (_locker)
            {
                _numberOfRunningThreads = 0;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_locker)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
            }

            if (!disposing)
                return;

            AbortAll();

            //Under the lock so a finishing thread in RunAction() cannot Set() the event after it is disposed
            lock (_locker)
            {
                _resetEvent.Set();//release any DoWork() caller waiting for a free thread
                _resetEvent.Dispose();
            }
        }

        public virtual bool HasRunningThreads()
        {
            lock (_locker)
            {
                return _numberOfRunningThreads > 0;
            }
        }

        protected virtual void RunAction(Action action, bool decrementRunningThreadCountOnCompletion = true)
        {
            try
            {
                action.Invoke();
                Log.Debug("Action completed successfully.");
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Thread cancelled.");
                throw;
            }
            catch (Exception e)
            {
                Log.Error("Error occurred while running action: {0}", e);
            }
            finally
            {
                if (decrementRunningThreadCountOnCompletion)
                {
                    lock (_locker)
                    {
                        _numberOfRunningThreads--;
                        Log.Debug("[{0}] threads are running.", _numberOfRunningThreads);
                        if (!_isDisposed && _numberOfRunningThreads < MaxThreads)
                            _resetEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Runs the action on a separate thread
        /// </summary>
        protected abstract void RunActionOnDedicatedThread(Action action);
    }
}
