using Abot2.Util;
using System;

namespace Abot2.Core
{
    public class BloomFilterCrawledUrlRepository : ICrawledUrlRepository
    {
        protected IBloomFilter<string> BloomFilter { get; set; }

        public BloomFilterCrawledUrlRepository()
            :this(null)
        {

        }

        public BloomFilterCrawledUrlRepository(IBloomFilter<string> bloomFilter)
        {
            BloomFilter = bloomFilter ?? new BloomFilter<string>(2000001, 0.001F);
        }
        
        public bool Contains(Uri uri)
        {
            if (uri == null)
                return false;

            return BloomFilter.Contains(uri.AbsoluteUri);
        }

        public bool AddIfNew(Uri uri)
        {
            if (uri == null)
                return false;

            if (BloomFilter.Contains(uri.AbsoluteUri))
                return false;

            BloomFilter.Add(uri.AbsoluteUri);

            return true;    
        }

        public void Dispose()
        {
           
            //BloomFilter = null;
        }
    }
}
