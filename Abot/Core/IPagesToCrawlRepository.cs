using Abot.Poco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Abot.Core
{
    public interface IPagesToCrawlRepository
    {
        void Add(PageToCrawl page);
        PageToCrawl GetNext();
        void Clear();
        int Count();

    }
}
