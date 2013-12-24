using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Abot.Core
{
    public class OnDiskCrawledUrlRepository : ICrawledUrlRepository
    {
        ICrawledUrlRepository _memoryUrlRepositoryCache;
        IHashGenerator _hashGenerator;
        ConcurrentQueue<Uri> _memoryURLRepositoryForWriting = new ConcurrentQueue<Uri>();
        volatile bool _creatingDirectory = false;
        object _directoryLocker = new object();
        int _threadSleep = 5000;
        bool _useInMemoryCache = true;
        string _directoryName;
        CancellationTokenSource _cancellationToken;
        
        static readonly string[] HexStringTable = new string[]{
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F",
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F",
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F",
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F",
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F",
            "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF",
            "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF",
            "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF",
            "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF",
            "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF",
            "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF"
        };

        public OnDiskCrawledUrlRepository(IHashGenerator hashGenerator, int watcherDelayInMS = 5000, bool useInMemoryCache = true, string directoryName = null)
        {
            _directoryName = directoryName ?? "CrawledUrls_" + Guid.NewGuid();

            if (useInMemoryCache)
            {
                _useInMemoryCache = true;
                _memoryUrlRepositoryCache = new InMemoryCrawledUrlRepository();
            }
            if (Directory.Exists(_directoryName))
            {
                DeleteDirectory(_directoryName);
            }
            Directory.CreateDirectory(_directoryName);

            _cancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew(() => monitorDisk(), _cancellationToken.Token);

            _hashGenerator = hashGenerator;
        }

        public bool Contains(Uri uri)
        {
            if (_useInMemoryCache)
            {
                if (_memoryUrlRepositoryCache.Contains(uri))
                {
                    if (_memoryURLRepositoryForWriting.Contains(uri))
                    {
                        return true;
                    }
                    return Contains(filePath(uri));
                }
            }
            else
            {
                if (_memoryURLRepositoryForWriting.Contains(uri))
                {
                    return true;
                }
                return Contains(filePath(uri));
            }
            return false;
        }

        public bool AddIfNew(Uri uri)
        {
            if (Contains(uri))
            {
                return false;
            }
            else
            {
                _memoryUrlRepositoryCache.AddIfNew(uri);
                _memoryURLRepositoryForWriting.Enqueue(uri);
                return true;
            }
        }

        public string ToHex(byte[] value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (value != null)
            {
                foreach (byte b in value)
                {
                    stringBuilder.Append(HexStringTable[b]);
                }
            }

            return stringBuilder.ToString();
        }

        public virtual void Dispose()
        {
            _cancellationToken.Cancel();
            
            if (Directory.Exists(_directoryName))
                DeleteDirectory(_directoryName);
        }

        protected bool Contains(string path)
        {
            while (_creatingDirectory == true)
                Thread.Sleep(100);

            return Directory.Exists(path);
        }
        
        protected void monitorDisk()
        {
            while (true)
            {
                if (_cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    Uri cUri = null;
                    while (_memoryURLRepositoryForWriting.TryDequeue(out cUri))
                        AddIfNewDisk(cUri);
                }
                catch
                {
                }

                Thread.Sleep(_threadSleep);
            }
        }

        protected bool AddIfNewDisk(Uri uri)
        {
            var directoryName = filePath(uri);
            if (Contains(directoryName))
            {
                return false;
            }
            else
            {
                try
                {
                    _creatingDirectory = true;
                    Directory.CreateDirectory(directoryName);
                }
                catch
                {
                }
                finally
                {
                    _creatingDirectory = false;
                }
                return true;
            }

        }

        protected string filePath(Uri uri)
        {
            var directoryName = ToHex(_hashGenerator.GenerateHash(ASCIIEncoding.ASCII.GetBytes(uri.AbsoluteUri)));

            return Path.Combine(_directoryName, uri.Authority, directoryName.Substring(0, 4), directoryName);
        }

        private void DeleteDirectory(string path)
        {
            //try
            //{
            //    Directory.Delete(path, false);
            //}
            //catch (IOException)
            //{
            //    Thread.Sleep(0);
            //    Directory.Delete(path, false);
            //}

            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(path, false);
        }
    }
}

