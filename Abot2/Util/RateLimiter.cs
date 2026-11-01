using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Abot2.Util
{
    [Obsolete("Abot no longer uses this; it rate limits per domain with DomainRateLimiter.RateLimitAsync, which waits without blocking a thread. For general-purpose rate limiting use System.Threading.RateLimiting.")]
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
    [Obsolete("Abot no longer uses this; it rate limits per domain with DomainRateLimiter.RateLimitAsync, which waits without blocking a thread. For general-purpose rate limiting use System.Threading.RateLimiting.")]
    public class RateLimiter : IRateLimiter, IDisposable
    {
        // Semaphore used to count and limit the number of occurrences per
        // unit time.
        private readonly SemaphoreSlim _semaphore;

        // Clock and timers. TimeProvider.System is precise on every platform, unlike Environment.TickCount,
        // which on .NET Framework only advances every ~15 ms and let waits end early. Tests pass a fake one.
        private readonly TimeProvider _timeProvider;

        // Timestamp the exit times are measured from.
        private readonly long _startTimestamp;

        // Times (in ticks since _startTimestamp) at which the semaphore should be exited.
        private readonly ConcurrentQueue<long> _exitTimes;

        // Timer used to trigger exiting the semaphore. Null when the time unit is zero, as there is nothing to limit.
        private readonly ITimer _exitTimer;

        // Whether this instance is disposed (1) or not (0). An int so Dispose can set it atomically.
        private int _isDisposed;

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
            : this(occurrences, timeUnit, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="RateLimiter"/> with a rate of <paramref name="occurrences"/>
        /// per <paramref name="timeUnit"/>, measured with <paramref name="timeProvider"/>.
        /// </summary>
        /// <param name="occurrences">Number of occurrences allowed per unit of time.</param>
        /// <param name="timeUnit">Length of the time unit.</param>
        /// <param name="timeProvider">Clock and timers to use; if null, the system clock (TimeProvider.System). Pass a fake one to control time in tests.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="occurrences"/> or <paramref name="timeUnit"/> is negative.
        /// </exception>
        public RateLimiter(int occurrences, TimeSpan timeUnit, TimeProvider timeProvider)
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

            _timeProvider = timeProvider ?? TimeProvider.System;
            _startTimestamp = _timeProvider.GetTimestamp();

            // Create the semaphore, with the number of occurrences as the maximum count.
            _semaphore = new SemaphoreSlim(Occurrences, Occurrences);

            // Create a queue to hold the semaphore exit times.
            _exitTimes = new ConcurrentQueue<long>();

            // Create a timer to exit the semaphore. Use the time unit as the original
            // interval length because that's the earliest we will need to exit the semaphore.
            // A zero time unit limits nothing, and a timer rescheduling itself with no delay would keep a thread busy.
            if (TimeUnitMilliseconds > 0)
                _exitTimer = _timeProvider.CreateTimer(ExitTimerCallback, null, TimeSpan.FromMilliseconds(TimeUnitMilliseconds), Timeout.InfiniteTimeSpan);
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

            if (_exitTimer == null)
                return true;

            // Block until we can enter the semaphore or until the timeout expires.
            var entered = _semaphore.Wait(millisecondsTimeout);

            // If we entered the semaphore, compute the corresponding exit time 
            // and add it to the queue.
            if (entered)
            {
                var timeToExit = ElapsedTicks() + TimeSpan.FromMilliseconds(TimeUnitMilliseconds).Ticks;
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
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (isDisposing)
            {
                // The semaphore and timer both implement IDisposable and
                // therefore must be disposed. The timer goes first so no new
                // callbacks start; one that is already running is handled in ExitTimerCallback.
                _exitTimer?.Dispose();
                _semaphore.Dispose();
            }
        }

        // Callback for the exit timer that exits the semaphore based on exit times
        // in the queue and then sets the timer for the nextexit time.
        private void ExitTimerCallback(object state)
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                return;

            try
            {
                // While there are exit times that are passed due still in the queue,
                // exit the semaphore and dequeue the exit time.
                long exitTime;
                var exitTimeValid = _exitTimes.TryPeek(out exitTime);
                while (exitTimeValid)
                {
                    if (exitTime - ElapsedTicks() > 0)
                    {
                        break;
                    }
                    _semaphore.Release();
                    _exitTimes.TryDequeue(out exitTime);
                    exitTimeValid = _exitTimes.TryPeek(out exitTime);
                }

                // we are already holding the next item from the queue, do not peek again
                // although this exit time may have already pass by this stmt.
                var timeUnit = TimeSpan.FromMilliseconds(TimeUnitMilliseconds);
                var timeUntilNextCheck = exitTimeValid ? TimeSpan.FromTicks(Math.Min(timeUnit.Ticks, Math.Max(0, exitTime - ElapsedTicks()))) : timeUnit;

                _exitTimer.Change(timeUntilNextCheck, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // Disposed while this callback was running. An unhandled exception on a
                // timer thread would crash the process, so there is nothing more to do.
            }
        }

        private long ElapsedTicks()
        {
            return _timeProvider.GetElapsedTime(_startTimestamp).Ticks;
        }

        // Throws an ObjectDisposedException if this object is disposed.
        private void CheckDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
