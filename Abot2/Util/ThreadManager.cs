using System;
using System.Threading;
using Serilog;

namespace Abot2.Util
{
    /// <summary>
    /// Handles the multithreading implementation details
    /// </summary>
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

    public abstract class ThreadManager : IThreadManager
    {
        protected bool _abortAllCalled = false;
        protected int _numberOfRunningThreads = 0;
        protected ManualResetEvent _resetEvent = new ManualResetEvent(true);
        protected object _locker = new object();
        protected bool _isDisplosed = false;

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

            if (_abortAllCalled)
                throw new InvalidOperationException("Cannot call DoWork() after AbortAll() or Dispose() have been called.");

            if (!_isDisplosed && MaxThreads > 1)
            {
                _resetEvent.WaitOne();
                lock (_locker)
                {
                    _numberOfRunningThreads++;
                    if (!_isDisplosed && _numberOfRunningThreads >= MaxThreads)
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

        public virtual void Dispose()
        {
            AbortAll();
            _resetEvent.Dispose();
            _isDisplosed = true;
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
                        if (!_isDisplosed && _numberOfRunningThreads < MaxThreads)
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
