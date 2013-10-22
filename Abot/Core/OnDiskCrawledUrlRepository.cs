using System;

namespace Abot.Core
{
    public class OnDiskCrawledUrlRepository : ICrawledUrlRepository
    {
        public bool Contains(Uri uri)
        {
            throw new NotImplementedException();
        }

        public bool AddIfNew(Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}
