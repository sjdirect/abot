using Abot2.Util;
using System;
using System.Collections.Concurrent;

namespace Abot2.Core
{
    /// <summary>
    /// Implementation that stores a numeric hash of the url instead of the url itself to use for lookups. This should save space when the crawled url list gets very long. 
    /// </summary>
    public class CompactCrawledUrlRepository : ICrawledUrlRepository
    {
        private ConcurrentDictionary<long, byte> _mUrlRepository = new ConcurrentDictionary<long, byte>();

        /// <inheritDoc />
        public bool Contains(Uri uri)
        {
            return GetRepository().ContainsKey(GetKey(uri));
        }

        /// <inheritDoc />
        public bool AddIfNew(Uri uri)
        {
            return GetRepository().TryAdd(GetKey(uri), 0);
        }

        /// <inheritDoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _mUrlRepository = null;
        }

        /// <summary>
        /// A 64-bit hash that is the same in every process, unlike Uri.GetHashCode(), which is only 32 bits and seeded randomly per process on .NET.
        /// It covers the parts of the uri that Uri.Equals compares (not user info or fragment), so the same uris count as duplicates.
        /// </summary>
        private static long GetKey(Uri uri)
        {
            var value = uri.IsAbsoluteUri
                ? uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped)
                : uri.OriginalString;

            return StableHash.Hash64(value);
        }

        private ConcurrentDictionary<long, byte> GetRepository()
        {
            return _mUrlRepository ?? throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
