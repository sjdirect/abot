using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElmahFiddler;
using Fiddler;
using Ionic.Zip;

namespace Abot.Tests.Unit
{
    public class SazImporter
    {
        public static IEnumerable<Session> ReadSessionArchive(string sazFile)
        {
            Dictionary<string, SazZipFileGroup> zipFileGroups = new Dictionary<string, SazZipFileGroup>();
            using (var zip = ZipFile.Read(sazFile))
            {
                foreach (var file in zip.Entries.Where(z => z.FileName.EndsWith(".txt") || z.FileName.EndsWith("_m.xml") ))
                {
                    var key = file.FileName.Substring(0, file.FileName.IndexOf("_", StringComparison.Ordinal));
                    
                    SazZipFileGroup zipFileGroup;
                    if (!zipFileGroups.TryGetValue(key, out zipFileGroup))
                    {
                        zipFileGroup = new SazZipFileGroup();
                        zipFileGroups.Add(key, zipFileGroup);
                    }
                        

                    if (file.FileName.EndsWith("_c.txt"))
                        zipFileGroup.Request = file;
                    else if (file.FileName.EndsWith("_m.xml"))
                        zipFileGroup.Meta = file;
                    else if (file.FileName.EndsWith("_s.txt"))
                        zipFileGroup.Response = file;
                }

                //TODO return a dictionary but try to use the url as the key so i can be used by the caller
                //return zipFileGroups.ToDictionary(a => a.Key, b => new Session(b.Value.Request.ExtractWithPasswordToBytes(null), b.Value.Response.ExtractWithPasswordToBytes(null)));
                return
                    zipFileGroups.Select(
                        g =>
                            new Session(g.Value.Request.ExtractWithPasswordToBytes(null),
                                g.Value.Response.ExtractWithPasswordToBytes(null)));
            }
        }
    }

    public class SazZipFileGroup
    {
        public ZipEntry Request { get; set; }
        public ZipEntry Response { get; set; }
        public ZipEntry Meta { get; set; }
    }
}
