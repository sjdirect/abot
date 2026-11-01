using System.Text;

namespace Abot2.Util
{
    /// <summary>
    /// Hashes that are the same in every process, on every runtime and platform. string.GetHashCode() is not:
    /// .NET seeds it randomly per process, so it must not be used where a hash identifies a value or results must be reproducible.
    /// </summary>
    internal static class StableHash
    {
        const ulong FnvOffsetBasis = 14695981039346656037;
        const ulong FnvPrime = 1099511628211;

        /// <summary>
        /// 64-bit FNV-1a hash of the UTF-8 bytes of the value
        /// </summary>
        public static long Hash64(string value)
        {
            unchecked
            {
                var hash = FnvOffsetBasis;
                foreach (var b in Encoding.UTF8.GetBytes(value))
                {
                    hash ^= b;
                    hash *= FnvPrime;
                }
                return (long)hash;
            }
        }

        /// <summary>
        /// The 64-bit hash folded into 32 bits
        /// </summary>
        public static int Hash32(string value)
        {
            unchecked
            {
                var hash = Hash64(value);
                return (int)hash ^ (int)(hash >> 32);
            }
        }
    }
}
