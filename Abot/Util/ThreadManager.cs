using log4net;
using System;
using System.Threading;

namespace Abot.Util
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
        /// Will perform the action asynchrously on a seperate thread
        /// </summary>
        /// <param name="action">The action to perform</param>
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

    [Serializable]
    public abstract class ThreadManager : IThreadManager
    {
        protected static ILog _logger = LogManager.GetLogger("AbotLogger");
        protected bool _abortAllCalled = false;
        protected int _numberOfRunningThreads = 0;
        protected ManualResetEvent _resetEvent = new ManualResetEvent(true);
        protected Object _locker = new Object();
        protected bool _isDisplosed = false;

        public ThreadManager(int maxThreads)
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
        /// Will perform the action asynchrously on a seperate thread
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

                    _logger.DebugFormat("Starting another thread, increasing running threads to [{0}].", _numberOfRunningThreads);
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
            _numberOfRunningThreads = 0;
        }

        public virtual void Dispose()
        {
            AbortAll();
            _resetEvent.Dispose();
            _isDisplosed = true;
        }

        public virtual bool HasRunningThreads()
        {
            return _numberOfRunningThreads > 0;
        }

        protected virtual void RunAction(Action action, bool decrementRunningThreadCountOnCompletion = true)
        {
            try
            {
                action.Invoke();
                _logger.Debug("Action completed successfully.");
            }
            catch (OperationCanceledException)
            {
                _logger.DebugFormat("Thread cancelled.");
                throw;
            }
            catch (Exception e)
            {
                _logger.Error("Error occurred while running action.");
                _logger.Error(e);
            }
            finally
            {
                if (decrementRunningThreadCountOnCompletion)
                {
                    lock (_locker)
                    {
                        _numberOfRunningThreads--;
                        _logger.DebugFormat("[{0}] threads are running.", _numberOfRunningThreads);
                        if (!_isDisplosed && _numberOfRunningThreads < MaxThreads)
                            _resetEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Runs the action on a seperate thread
        /// </summary>
        protected abstract void RunActionOnDedicatedThread(Action action);
    }
}
