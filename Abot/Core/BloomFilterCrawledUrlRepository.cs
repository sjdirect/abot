﻿using Abot.Util;
using System;

namespace Abot.Core
{
    [Serializable]
    public class BloomFilterCrawledUrlRepository : ICrawledUrlRepository
    {
        protected IBloomFilter<string> BloomFilter { get; set; }

        public BloomFilterCrawledUrlRepository()
            :this(null)
        {

        }

        public BloomFilterCrawledUrlRepository(IBloomFilter<string> bloomFilter)
        {
            BloomFilter = bloomFilter ?? new BloomFilter<string>(100000, 0.01F);
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
