using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Abot.Poco;
using Abot.Util;
using log4net;

namespace Abot.Crawler
{
    public class AsyncWebCrawler : WebCrawler
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");

        protected virtual void CrawlSite()
        {
            // Create a scheduler that uses two threads. 
            LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(2);//TODO get this from config and set in constructor
            //List<Task> tasks = new List<Task>();

            // Create a TaskFactory and pass it our custom scheduler. 
            TaskFactory factory = new TaskFactory(lcts);

            Object lockObj = new Object();

            BlockingCollection<PageToCrawl> pagesToCrawl = new BlockingCollection<PageToCrawl>();

            foreach (PageToCrawl page in pagesToCrawl.GetConsumingEnumerable())
            {
                Task t = factory.StartNew(() =>
                {
                    ProcessPage(_scheduler.GetNext());
                }, _crawlContext.CancellationTokenSource.Token);
                
                //TODO Add t to some collection that keeps track of only x number
            }

            while (!_crawlComplete)
            {
                RunPreWorkChecks();

                // Use our factory to run a set of tasks. 
                int outputItem = 0;

                   //tasks.Add(t);

                // Wait for the tasks to complete before displaying a completion message.
                //Task.WaitAll(tasks.ToArray());
                //Console.WriteLine("\n\nSuccessful completion.");


                //if (_scheduler.Count > 0)
                //{
                //    _threadManager.DoWork(() => ProcessPage(_scheduler.GetNext()));
                    
                //}
                //else if (!_threadManager.HasRunningThreads())
                //{
                //    _crawlComplete = true;
                //}
                //else
                //{
                //    _logger.DebugFormat("Waiting for links to be scheduled...");
                //    Thread.Sleep(2500);
                //}
            }
        }

    }
}
