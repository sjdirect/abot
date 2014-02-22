using BloomFilterLib;
using System;

namespace Abot.Core
{

    public class MemoryBloomUrlRepository : ICrawledUrlRepository, IDisposable
    {
        BloomFilter _urlRepository = null;


        public MemoryBloomUrlRepository(double falsePositiveProbability = .0001, int expectedElements = 100000000)
            : this(new BloomFilter(falsePositiveProbability, expectedElements))
        {
            
        }

        public MemoryBloomUrlRepository(BloomFilter bloomFilter)
        {
            _urlRepository = bloomFilter;
        }
        
        ~MemoryBloomUrlRepository()
        {
            Dispose();
        }


        public virtual void Dispose()
        {
            _urlRepository = null;
        }

        public bool Contains(Uri uri)
        {
            return _urlRepository.contains(uri.AbsoluteUri);
        }
        
        public bool AddIfNew(Uri uri)
        {
            if (!Contains(uri))
            {
                _urlRepository.add(uri.AbsoluteUri);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Contains(string uri)
        {
            return _urlRepository.contains(uri);
        }

        public bool AddIfNew(string uri)
        {
            if (!Contains(uri))
            {
                _urlRepository.add(uri);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string BloomState
        {
            get
            {
                return _urlRepository.BloomFilterBinarySerialization();
            }
        }
    }
}
