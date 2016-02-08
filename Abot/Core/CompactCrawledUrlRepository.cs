using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Abot.Core
{
    /// <summary>
    /// Implementation that stores a numeric hash of the url instead of the url itself to use for lookups. This should save space when the crawled url list gets very long. 
    /// </summary>
    public class CompactCrawledUrlRepository : ICrawledUrlRepository
    {
        private ConcurrentDictionary<long, byte> m_UrlRepository = new ConcurrentDictionary<long, byte>();

        /// <inheritDoc />
        public bool Contains(Uri uri)
        {
            return m_UrlRepository.ContainsKey(ComputeNumericId(uri.AbsoluteUri));
        }

        /// <inheritDoc />
        public bool AddIfNew(Uri uri)
        {
            return m_UrlRepository.TryAdd(ComputeNumericId(uri.AbsoluteUri), 0);
        }

        /// <inheritDoc />
        public virtual void Dispose()
        {
            m_UrlRepository = null;
        }

        protected long ComputeNumericId(string p_Uri)
        {
            byte[] md5 = ToMd5Bytes(p_Uri);

            long numericId = 0;
            for (int i = 0; i < 8; i++)
            {
                numericId += (long)md5[i] << (i * 8);
            }

            return numericId;
        }

        protected byte[] ToMd5Bytes(string p_String)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.Default.GetBytes(p_String));
            }
        }
    }
}
