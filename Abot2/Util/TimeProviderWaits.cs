using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abot2.Util
{
    /// <summary>
    /// The framework's TimeProvider-aware waits, which are built in on .NET 8 and later and come from Microsoft.Bcl.TimeProvider on older frameworks
    /// </summary>
    internal static class TimeProviderWaits
    {
        /// <summary>
        /// A task that completes after the delay on the given clock, or is cancelled by the token
        /// </summary>
        public static Task Delay(TimeProvider timeProvider, TimeSpan delay, CancellationToken cancellationToken)
        {
#if NET
            return Task.Delay(delay, timeProvider, cancellationToken);
#else
            return TimeProviderTaskExtensions.Delay(timeProvider, delay, cancellationToken);
#endif
        }

        /// <summary>
        /// A token source that cancels itself after the delay on the given clock
        /// </summary>
        public static CancellationTokenSource CreateCancellationTokenSource(TimeProvider timeProvider, TimeSpan delay)
        {
#if NET
            return new CancellationTokenSource(delay, timeProvider);
#else
            return TimeProviderTaskExtensions.CreateCancellationTokenSource(timeProvider, delay);
#endif
        }
    }
}
