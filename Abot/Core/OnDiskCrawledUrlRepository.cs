using log4net;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Abot.Core
{
    public class OnDiskCrawledUrlRepository : ICrawledUrlRepository
    {
        static ILog _logger = LogManager.GetLogger(typeof(OnDiskCrawledUrlRepository).FullName);
        IHashGenerator _hashGenerator;
        BlockingCollection<Uri> _urisToWriteToDisk = new BlockingCollection<Uri>();
        CancellationTokenSource _cancellationToken;
        DirectoryInfo _uriDbDirectory;
        object _directoryLocker = new object();
        object _hashLocker = new object();
        bool _deleteUriDbOnDispose;

        /// <summary>
        /// Uses file system as a db of crawled uris. Creates directories to represent a url crawled. This prevents OutOfMemoryException
        /// by storing crawled uris to disk instead of holding them all in memory. This class should only be used when you expect a crawler
        /// to encounter hundreds of thousands of links during a single crawl. Otherwise use the InMemoryCrawledUrlRepository
        /// </summary>
        public OnDiskCrawledUrlRepository()
            :this(null)
        {
        }

        /// <summary>
        /// Uses file system as a db of crawled uris. Creates directories to represent a url crawled. This prevents OutOfMemoryException
        /// by storing crawled uris to disk instead of holding them all in memory. This class should only be used when you expect a crawler
        /// to encounter hundreds of thousands of links during a single crawl. Otherwise use the InMemoryCrawledUrlRepository
        /// </summary>
        /// <param name="hashGenerator">Generates hashes from uris to be used as directory names</param>
        /// <param name="uriDbDirectory">Directory to use as the parent. Will create directories to represent crawled uris in this directory.</param>
        /// <param name="deleteUriDbDirectoryOnDispose">Whether the uriDbDirectory should be deleted after the crawl completes</param>
        public OnDiskCrawledUrlRepository(IHashGenerator hashGenerator, DirectoryInfo uriDbDirectory = null, bool deleteUriDbDirectoryOnDispose = false)
        {
            _hashGenerator = hashGenerator ?? new Murmur3HashGenerator();

            if (uriDbDirectory == null)
                _uriDbDirectory = new DirectoryInfo("UriDb");
            else
                _uriDbDirectory = uriDbDirectory;

            if (_uriDbDirectory.Exists)
            {
                _logger.WarnFormat("The directory [{0}] already exists and will be reused as the disk crawled url db. Any urls already stored there will not be recrawled", _uriDbDirectory.FullName);
            }
            else
            {
                _logger.InfoFormat("Creating directory [{0}] and will use as the on disk crawled url db", _uriDbDirectory.FullName);
                _uriDbDirectory.Create();                
            }
                
            _deleteUriDbOnDispose = deleteUriDbDirectoryOnDispose;

            _cancellationToken = new CancellationTokenSource();
            Task.Factory.StartNew(() => WriteUrisToDisk(), _cancellationToken.Token);
        }

        public bool Contains(Uri uri)
        {
            return _urisToWriteToDisk.Contains(uri) || DirectoryExists(GetFilePath(uri));
        }

        public bool AddIfNew(Uri uri)
        {
            if (Contains(uri))
                return false;

            _urisToWriteToDisk.TryAdd(uri);
            return true;
        }

        public virtual void Dispose()
        {
            _cancellationToken.Cancel();
            _urisToWriteToDisk = new BlockingCollection<Uri>();

            if (_deleteUriDbOnDispose && Directory.Exists(_uriDbDirectory.FullName))
            {
                _logger.InfoFormat("Deleting directory [{0}] that was used as the crawled url db", _uriDbDirectory.FullName);
                DeleteDirectory();
            }
        }

        protected bool DirectoryExists(string path)
        {
            lock (_directoryLocker)
            {
                return Directory.Exists(path);
            }
        }
        
        protected void WriteUrisToDisk()
        {
            foreach(Uri uri in _urisToWriteToDisk.GetConsumingEnumerable())
            {
                if (_cancellationToken.IsCancellationRequested)
                    break;

                CreateDirectoryIfNew(uri);
                System.Threading.Thread.Sleep(100);
            }
        }

        protected bool CreateDirectoryIfNew(Uri uri)
        {
            var directoryName = GetFilePath(uri);
            if (DirectoryExists(directoryName))
                return false;

            lock (_directoryLocker)
            {
                try
                {
                    Directory.CreateDirectory(directoryName);
                    _logger.DebugFormat("Creating directory [{0}] for uri [{1}]", directoryName, uri);
                }
                catch(Exception e)
                {
                    _logger.WarnFormat("Error Creating directory [{0}] for uri [{1}]: {2}", directoryName, uri, e);
                }
            }
            return true;
        }

        protected string GetFilePath(Uri uri)
        {
            string hashedDirectoryName = "";
            lock (_hashLocker)
            {
                hashedDirectoryName = BitConverter.ToString(_hashGenerator.GenerateHash(ASCIIEncoding.ASCII.GetBytes(uri.AbsoluteUri)));
            }
            return Path.Combine(_uriDbDirectory.FullName, uri.Host, hashedDirectoryName.Substring(0, 4), hashedDirectoryName);
        }

        protected void DeleteDirectory()
        {
            //Had to take this approach due to issues with Directory.Delete(path, true) throwing exceptions, see link below
            //http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
            //The Process approach is the only one that will cleanup
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + @"rmdir /s/q " + _uriDbDirectory.FullName;
            process.Start();
            process.WaitForExit();
        }
    }
}

