using System;
using System.Collections.Concurrent;

namespace Abot2.Core
{
    public interface ICrawledUrlRepository : IDisposable
    {
        bool Contains(Uri uri);
        bool AddIfNew(Uri uri);
    }

    public class InMemoryCrawledUrlRepository : ICrawledUrlRepository
    {
        ConcurrentDictionary<string, byte> _urlRepository = new ConcurrentDictionary<string, byte>();

        public bool Contains(Uri uri)
        {
            return GetRepository().ContainsKey(uri.AbsoluteUri);
        }

        public bool AddIfNew(Uri uri)
        {
            return GetRepository().TryAdd(uri.AbsoluteUri, 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _urlRepository = null;
        }

        private ConcurrentDictionary<string, byte> GetRepository()
        {
            return _urlRepository ?? throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
