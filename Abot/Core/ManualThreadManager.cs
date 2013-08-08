using log4net;
using System;
using System.Threading;

namespace Abot.Core
{
    public class ManualThreadManager : IThreadManager
    {
        static ILog _logger = LogManager.GetLogger(typeof(ManualThreadManager).FullName);
        object _lock = new object();
        Thread[] _threads = new Thread[10];
        bool _abortAllCalled = false;

        public ManualThreadManager(int maxThreads)
        {
            if ((maxThreads > 100) || (maxThreads < 1))
                throw new ArgumentException("MaxThreads must be from 1 to 100");
            else
                _threads = new Thread[maxThreads];
        }

        /// <summary>
        /// Max number of threads to use
        /// </summary>
        public int MaxThreads
        {
            get
            {
                return _threads.Length;
            }
        }

        /// <summary>
        /// Will perform the action asynchrously on a seperate thread
        /// </summary>
        public void DoWork(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (_abortAllCalled)
                throw new InvalidOperationException("Cannot call DoWork() after AbortAll() or Dispose() have been called.");

            lock (_lock)
            {
                int freeThreadIndex = GetFreeThreadIndex();
                while (freeThreadIndex < 0)
                {
                    _logger.Debug("Waiting for a free thread to do work, sleeping 100 millisec");
                    System.Threading.Thread.Sleep(100);
                    freeThreadIndex = GetFreeThreadIndex();
                }

                if (MaxThreads > 1)
                {
                    _threads[freeThreadIndex] = new Thread(new ThreadStart(action));
                    _logger.DebugFormat("Doing work on thread Index:[{0}] Id[{1}]", freeThreadIndex, _threads[freeThreadIndex].ManagedThreadId);
                    _threads[freeThreadIndex].Start();
                }
                else
                {
                    action.Invoke();
                }
            }
        }

        public void AbortAll()
        {
            //Do nothing
            _abortAllCalled = true;
        }

        public void Dispose()
        {
            AbortAll();
        }

        private object DoNothing()
        {
            return null;
        }

        /// <summary>
        /// Whether there are running threads
        /// </summary>
        public bool HasRunningThreads()
        {
            lock (_lock)
            {
                for (int i = 0; i < _threads.Length; i++)
                {
                    if (_threads[i] == null)
                    {
                        _logger.DebugFormat("Thread Null Index:[{0}]", i);
                    }
                    else if (_threads[i].IsAlive)
                    {
                        _logger.DebugFormat("Thread Is Running Index:[{0}] Id:[{1}] State:[{2}]", i, _threads[i].ManagedThreadId, _threads[i].ThreadState);
                        return true;
                    }
                    else
                    {
                        _logger.DebugFormat("Thread Not Running Index:[{0}] Id:[{1}] State:[{2}]", i, _threads[i].ManagedThreadId, _threads[i].ThreadState);
                    }
                }
            }

            _logger.DebugFormat("No Threads Running!!");
            return false;
        }

        private int GetFreeThreadIndex()
        {
            int freeThreadIndex = -1;
            int currentIndex = 0;
            lock (_lock)
            {
                foreach (Thread thread in _threads)
                {
                    if ((thread == null) || !thread.IsAlive)
                    {
                        freeThreadIndex = currentIndex;
                        break;
                    }

                    currentIndex++;
                }
            }
            return freeThreadIndex; ;
        }
    }
}