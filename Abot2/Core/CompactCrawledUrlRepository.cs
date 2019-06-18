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
            return _mUrlRepository.ContainsKey(uri.GetHashCode());
        }

        /// <inheritDoc />
        public bool AddIfNew(Uri uri)
        {
            return _mUrlRepository.TryAdd(uri.GetHashCode(), 0);
        }

        /// <inheritDoc />
        public virtual void Dispose()
        {
            _mUrlRepository = null;
        }
    }
}
