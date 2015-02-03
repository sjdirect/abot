﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abot.Util
{

    /// <summary>
    /// A ThreadManager implementation that will use tpl Tasks to handle concurrency.
    /// </summary>
    [Serializable]
    public class TaskThreadManager : ThreadManager
    {
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TaskThreadManager(int maxConcurrentTasks)
            :this(maxConcurrentTasks, null)
        {
        }

        public TaskThreadManager(int maxConcurrentTasks, CancellationTokenSource cancellationTokenSource)
            : base(maxConcurrentTasks)
        {
            _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
        }

        public override void AbortAll()
        {
            base.AbortAll();
            _cancellationTokenSource.Cancel();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
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
            if (task == null || task.Exception == null) 
                return;

            var aggException = task.Exception.Flatten();
            foreach (var exception in aggException.InnerExceptions)
            {
                if(_cancellationTokenSource.IsCancellationRequested)
                    _logger.Warn(exception);//If the task was cancelled then this exception is expected happen and we dont care
                else
                    _logger.Error(exception);//If the task was not cancelled then this is an error
            }
        }
    }
}
