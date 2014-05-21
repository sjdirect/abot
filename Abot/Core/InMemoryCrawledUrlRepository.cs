﻿using System;
using System.Collections.Concurrent;

namespace Abot.Core
{
    public interface ICrawledUrlRepository : IDisposable
    {
        bool Contains(Uri uri);
        bool AddIfNew(Uri uri);
    }

    [Obsolete("Will be removed in future release. Use MemoryBloomUrlRepository instead.")]
    public class InMemoryCrawledUrlRepository : ICrawledUrlRepository
    {
        ConcurrentDictionary<string, byte> _urlRepository = new ConcurrentDictionary<string, byte>();

        public bool Contains(Uri uri)
        {
            return _urlRepository.ContainsKey(uri.AbsoluteUri);
        }

        public bool AddIfNew(Uri uri)
        {
            return _urlRepository.TryAdd(uri.AbsoluteUri, 0);
        }

        public virtual void Dispose()
        {
            _urlRepository = null;
        }
    }
}
