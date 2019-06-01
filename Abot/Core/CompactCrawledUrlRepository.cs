using System;
using System.Collections.Concurrent;

namespace Abot.Core
{
    /// <summary>
    /// Implementation that stores a numeric hash of the url instead of the url itself to use for lookups. This should save space when the crawled url list gets very long. 
    /// </summary>
    public class CompactCrawledUrlRepository : ICrawledUrlRepository
    {
        private ConcurrentDictionary<int, byte> m_UrlRepository = new ConcurrentDictionary<int, byte>();

        /// <inheritDoc />
        public bool Contains(Uri uri)
        {
            return m_UrlRepository.ContainsKey(uri.GetHashCode());
        }

        /// <inheritDoc />
        public bool AddIfNew(Uri uri)
        {
            return m_UrlRepository.TryAdd(uri.GetHashCode(), 0);
        }

        /// <inheritDoc />
        public virtual void Dispose()
        {
            m_UrlRepository = null;
        }
    }
}
