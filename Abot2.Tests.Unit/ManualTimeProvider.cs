using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit
{
    /// <summary>
    /// A clock that only moves when <see cref="Advance"/> is called. Timers fire during Advance(), in order of their due time,
    /// once the clock reaches it. Microsoft's FakeTimeProvider does the same but does not support .NET Framework, which these tests also run on.
    /// </summary>
    internal sealed class ManualTimeProvider : TimeProvider
    {
        static readonly DateTimeOffset Start = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        readonly object _lock = new object();
        readonly List<ManualTimer> _scheduledTimers = new List<ManualTimer>();
        long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        /// <summary>
        /// Raised whenever a timer is scheduled, so a test can wait until the code under test has started waiting before it advances the clock
        /// </summary>
        public event Action TimerScheduled;

        public override long GetTimestamp()
        {
            lock (_lock)
                return _ticks;
        }

        public override DateTimeOffset GetUtcNow()
        {
            lock (_lock)
                return Start.AddTicks(_ticks);
        }

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }

        /// <summary>
        /// Moves the clock forward, firing every timer that becomes due on the way
        /// </summary>
        public void Advance(TimeSpan delta)
        {
            long target;
            lock (_lock)
                target = _ticks + delta.Ticks;

            while (true)
            {
                ManualTimer due = null;
                lock (_lock)
                {
                    foreach (var timer in _scheduledTimers)
                    {
                        if (timer.DueTicks <= target && (due == null || timer.DueTicks < due.DueTicks))
                            due = timer;
                    }

                    if (due == null)
                    {
                        _ticks = target;
                        return;
                    }

                    _ticks = Math.Max(_ticks, due.DueTicks);
                    due.Fired();
                }

                //Outside the lock, as the callback may change its timer
                due.Invoke();
            }
        }

        sealed class ManualTimer : ITimer
        {
            readonly ManualTimeProvider _owner;
            readonly TimerCallback _callback;
            readonly object _state;
            long _periodTicks;
            bool _disposed;

            public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object state)
            {
                _owner = owner;
                _callback = callback;
                _state = state;
            }

            /// <summary>Clock ticks at which the timer fires, while it is scheduled</summary>
            public long DueTicks { get; private set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_owner._lock)
                {
                    _owner._scheduledTimers.Remove(this);
                    if (_disposed)
                        return false;
                    if (dueTime == Timeout.InfiniteTimeSpan)
                        return true;

                    DueTicks = _owner._ticks + dueTime.Ticks;
                    _periodTicks = period == Timeout.InfiniteTimeSpan ? 0 : period.Ticks;
                    _owner._scheduledTimers.Add(this);
                }

                _owner.TimerScheduled?.Invoke();
                return true;
            }

            //Called under the owner's lock when the timer fires: reschedules a periodic timer, unschedules a one-off
            public void Fired()
            {
                _owner._scheduledTimers.Remove(this);
                if (_periodTicks > 0)
                {
                    DueTicks += _periodTicks;
                    _owner._scheduledTimers.Add(this);
                }
            }

            public void Invoke()
            {
                _callback(_state);
            }

            public void Dispose()
            {
                lock (_owner._lock)
                {
                    _disposed = true;
                    _owner._scheduledTimers.Remove(this);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }
}
