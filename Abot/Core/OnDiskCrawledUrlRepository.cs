using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        const string _rootDirectoryName = "UriDb";
        CancellationTokenSource _cancellationToken;
        bool _deleteUriDbOnDispose;

        public OnDiskCrawledUrlRepository(IHashGenerator hashGenerator, int watcherDelayInMS = 5000, bool useInMemoryCache = true, bool deleteUriDbOnDispose = false)
        {
            if (!Directory.Exists(_rootDirectoryName))
                Directory.CreateDirectory(_rootDirectoryName);
            
            if (useInMemoryCache)
            {
                _useInMemoryCache = true;
                _memoryUrlRepositoryCache = new InMemoryCrawledUrlRepository();
            }

            _cancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew(() => MonitorDisk(), _cancellationToken.Token);

            _hashGenerator = hashGenerator;
            _deleteUriDbOnDispose = deleteUriDbOnDispose;
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
                    return Contains(GetFilePath(uri));
                }
            }
            else
            {
                if (_memoryURLRepositoryForWriting.Contains(uri))
                {
                    return true;
                }
                return Contains(GetFilePath(uri));
            }
            return false;
        }

        public bool AddIfNew(Uri uri)
        {
            if (Contains(uri))
                return false;

            _memoryUrlRepositoryCache.AddIfNew(uri);
            _memoryURLRepositoryForWriting.Enqueue(uri);
            return true;
        }

        public virtual void Dispose()
        {
            _cancellationToken.Cancel();
            _memoryURLRepositoryForWriting = new ConcurrentQueue<Uri>();

            if (_deleteUriDbOnDispose && Directory.Exists(_rootDirectoryName))
                DeleteDirectory(_rootDirectoryName);
        }

        protected bool Contains(string path)
        {
            while (_creatingDirectory == true)
                Thread.Sleep(100);

            return Directory.Exists(path);
        }
        
        protected void MonitorDisk()
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
            var directoryName = GetFilePath(uri);
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

        protected string GetFilePath(Uri uri)
        {
            var directoryName = BitConverter.ToString(_hashGenerator.GenerateHash(ASCIIEncoding.ASCII.GetBytes(uri.AbsoluteUri)));
            return Path.Combine(_rootDirectoryName, uri.Authority, directoryName.Substring(0, 4), directoryName);
        }


        protected void DeleteDirectory(string path)
        {
            //Had to take this approach due to issues with Directory.Delete(path, true) throwing exceptions, see link below
            //http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
            //The Process approach is the only one that will cleanup
            
            string absPath = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
            
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + @"rmdir /s/q " + absPath;
            process.Start();
            process.WaitForExit();
        }
    }
}

