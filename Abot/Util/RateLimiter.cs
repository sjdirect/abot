﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Abot.Util
{
    public interface IRateLimiter
    {
        void WaitToProceed();
    }
    //http://www.pennedobjects.com/2010/10/better-rate-limiting-with-dot-net/
    /// <summary>
    /// Used to control the rate of some occurrence per unit of time.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     To control the rate of an action using a <see cref="RateLimiter"/>, 
    ///     code should simply call <see cref="WaitToProceed()"/> prior to 
    ///     performing the action. <see cref="WaitToProceed()"/> will block
    ///     the current thread until the action is allowed based on the rate 
    ///     limit.
    ///     </para>
    ///     <para>
    ///     This class is thread safe. A single <see cref="RateLimiter"/> instance 
    ///     may be used to control the rate of an occurrence across multiple 
    ///     threads.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class RateLimiter : IRateLimiter, IDisposable
    {
        // Semaphore used to count and limit the number of occurrences per
        // unit time.
        private readonly SemaphoreSlim _semaphore;

        // Times (in millisecond ticks) at which the semaphore should be exited.
        private readonly ConcurrentQueue<int> _exitTimes;

        // Timer used to trigger exiting the semaphore.
        private readonly Timer _exitTimer;

        // Whether this instance is disposed.
        private bool _isDisposed;

        /// <summary>
        /// Number of occurrences allowed per unit of time.
        /// </summary>
        public int Occurrences { get; private set; }

        /// <summary>
        /// The length of the time unit, in milliseconds.
        /// </summary>
        public int TimeUnitMilliseconds { get; private set; }

        /// <summary>
        /// Initializes a <see cref="RateLimiter"/> with a rate of <paramref name="occurrences"/> 
        /// per <paramref name="timeUnit"/>.
        /// </summary>
        /// <param name="occurrences">Number of occurrences allowed per unit of time.</param>
        /// <param name="timeUnit">Length of the time unit.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="occurrences"/> or <paramref name="timeUnit"/> is negative.
        /// </exception>
        public RateLimiter(int occurrences, TimeSpan timeUnit)
        {
            // Check the arguments.
            if (occurrences <= 0)
                throw new ArgumentOutOfRangeException("occurrences", "Number of occurrences must be a positive integer");
            if (timeUnit != timeUnit.Duration())
                throw new ArgumentOutOfRangeException("timeUnit", "Time unit must be a positive span of time");
            if (timeUnit >= TimeSpan.FromMilliseconds(UInt32.MaxValue))
                throw new ArgumentOutOfRangeException("timeUnit", "Time unit must be less than 2^32 milliseconds");

            Occurrences = occurrences;
            TimeUnitMilliseconds = (int)timeUnit.TotalMilliseconds;

            // Create the semaphore, with the number of occurrences as the maximum count.
            _semaphore = new SemaphoreSlim(Occurrences, Occurrences);

            // Create a queue to hold the semaphore exit times.
            _exitTimes = new ConcurrentQueue<int>();

            // Create a timer to exit the semaphore. Use the time unit as the original
            // interval length because that's the earliest we will need to exit the semaphore.
            _exitTimer = new Timer(ExitTimerCallback, null, TimeUnitMilliseconds, -1);
        }

        /// <summary>
        /// Blocks the current thread until allowed to proceed or until the
        /// specified timeout elapses.
        /// </summary>
        /// <param name="millisecondsTimeout">Number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>true if the thread is allowed to proceed, or false if timed out</returns>
        public bool WaitToProceed(int millisecondsTimeout)
        {
            // Check the arguments.
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout");

            CheckDisposed();

            // Block until we can enter the semaphore or until the timeout expires.
            var entered = _semaphore.Wait(millisecondsTimeout);

            // If we entered the semaphore, compute the corresponding exit time 
            // and add it to the queue.
            if (entered)
            {
                var timeToExit = unchecked(Environment.TickCount + TimeUnitMilliseconds);
                _exitTimes.Enqueue(timeToExit);
            }

            return entered;
        }

        /// <summary>
        /// Blocks the current thread until allowed to proceed or until the
        /// specified timeout elapses.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>true if the thread is allowed to proceed, or false if timed out</returns>
        public bool WaitToProceed(TimeSpan timeout)
        {
            return WaitToProceed((int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Blocks the current thread indefinitely until allowed to proceed.
        /// </summary>
        public void WaitToProceed()
        {
            WaitToProceed(Timeout.Infinite);
        }

        /// <summary>
        /// Releases unmanaged resources held by an instance of this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources held by an instance of this class.
        /// </summary>
        /// <param name="isDisposing">Whether this object is being disposed.</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!_isDisposed)
            {
                if (isDisposing)
                {
                    // The semaphore and timer both implement IDisposable and 
                    // therefore must be disposed.
                    _semaphore.Dispose();
                    _exitTimer.Dispose();

                    _isDisposed = true;
                }
            }
        }

        // Callback for the exit timer that exits the semaphore based on exit times 
        // in the queue and then sets the timer for the nextexit time.
        private void ExitTimerCallback(object state)
        {
            // While there are exit times that are passed due still in the queue,
            // exit the semaphore and dequeue the exit time.
            int exitTime;
            while (_exitTimes.TryPeek(out exitTime)
                    && unchecked(exitTime - Environment.TickCount) <= 0)
            {
                _semaphore.Release();
                _exitTimes.TryDequeue(out exitTime);
            }

            // Try to get the next exit time from the queue and compute
            // the time until the next check should take place. If the 
            // queue is empty, then no exit times will occur until at least
            // one time unit has passed.
            int timeUntilNextCheck;
            if (_exitTimes.TryPeek(out exitTime))
                timeUntilNextCheck = unchecked(exitTime - Environment.TickCount);
            else
                timeUntilNextCheck = TimeUnitMilliseconds;

            // Set the timer.
            _exitTimer.Change(timeUntilNextCheck, -1);
        }

        // Throws an ObjectDisposedException if this object is disposed.
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("RateGate is already disposed");
        }
    }
}
