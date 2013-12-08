using Abot.Poco;
using BloomFilter;
using System;

namespace Abot.Core
{
    /// <summary>
    /// In memory crawler that uses a bloom filter which is optimized for detecting if a url has been crawled before
    /// </summary>
    public class BloomCrawledUrlRepository : ICrawledUrlRepository
    {
        Filter<string> _urlRepository;
        object locker = new object();

        public BloomCrawledUrlRepository(CrawlConfiguration config)
        {
            int maxPages = (config.MaxPagesToCrawl <= int.MaxValue) ? Convert.ToInt32(config.MaxPagesToCrawl) : int.MaxValue;

            _urlRepository = new Filter<string>(maxPages, .001F, null);
        }

        /// <summary>
        /// Since this is a bloom filter, a return value of "true" cannot be trusted. There is only a guarantee that a return value of "false" is 100% accurate.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool Contains(Uri uri)
        {
            bool contains = false;
            
            lock (locker)
            {
                contains = _urlRepository.Contains(uri.AbsoluteUri);
            }

            return contains;
        }

        public bool AddIfNew(Uri uri)
        {
            bool added = false;

            lock(locker)
            {
                if (_urlRepository.Contains(uri.AbsoluteUri) == false)
                {
                    _urlRepository.Add(uri.AbsoluteUri);
                    added = true;
                }
            }

            return added;
        }

        public virtual void Dispose()
        {
            _urlRepository = null;
        }
    }
}
