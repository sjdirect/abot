﻿using Abot.Core;
using Abot.Poco;
using System;

namespace Abot.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            PrintDisclaimer();

            Uri uriToCrawl = GetSiteToCrawl(args);

            IWebCrawler crawler;

            //Uncomment only one of the following to see that instance in action
            crawler = GetDefaultWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            //crawler = GetCustomBehaviorUsingLambdaWebCrawler();

            //Subscribe to any of these synchronous/asynchronous events.
            //This is where you process data about specific events of the crawl
            crawler.PageRequesterEngine.PageRequestStarting += PageRequesterEngine_PageRequestStarting;
            crawler.PageRequesterEngine.PageRequestStartingAsync += PageRequesterEngine_PageRequestStartingAsync;
            crawler.PageRequesterEngine.PageRequestCompleted += PageRequesterEngine_PageRequestCompleted;
            crawler.PageRequesterEngine.PageRequestCompletedAsync += PageRequesterEngine_PageRequestCompletedAsync;

            crawler.PageProcessorEngine.PageProcessingStarting += PageProcessorEngine_PageProcessingStarting;
            crawler.PageProcessorEngine.PageProcessingStartingAsync += PageProcessorEngine_PageProcessingStartingAsync;
            crawler.PageProcessorEngine.PageCrawlDisallowed += PageProcessorEngine_PageCrawlDisallowed;
            crawler.PageProcessorEngine.PageCrawlDisallowedAsync += PageProcessorEngine_PageCrawlDisallowedAsync;
            crawler.PageProcessorEngine.PageLinksCrawlDisallowed += PageProcessorEngine_PageLinksCrawlDisallowed;
            crawler.PageProcessorEngine.PageLinksCrawlDisallowedAsync += PageProcessorEngine_PageLinksCrawlDisallowedAsync;
            crawler.PageProcessorEngine.PageProcessingCompleted += PageProcessorEngine_PageProcessingCompleted;
            crawler.PageProcessorEngine.PageProcessingCompletedAsync += PageProcessorEngine_PageProcessingCompletedAsync;

            //Start the crawl
            //This is a synchronous call
            CrawlResult result = crawler.Crawl(uriToCrawl);

            //Now go view the log.txt file that is in the same directory as this executable. It has
            //all the statements that you were trying to read in the console window :).
            //Not enough data being logged? Change the app.config file's log4net log level from "INFO" TO "DEBUG"

            PrintDisclaimer();
        }


        static void PageRequesterEngine_PageRequestStarting(            object sender, PageActionStartingArgs e){}
        static void PageRequesterEngine_PageRequestStartingAsync(       object sender, PageActionStartingArgs e){}
        static void PageRequesterEngine_PageRequestCompleted(           object sender, PageActionCompletedArgs e){}
        static void PageRequesterEngine_PageRequestCompletedAsync(      object sender, PageActionCompletedArgs e){}

        static void PageProcessorEngine_PageProcessingStarting(         object sender, PageActionStartingArgs e){}
        static void PageProcessorEngine_PageProcessingStartingAsync(    object sender, PageActionStartingArgs e){}
        static void PageProcessorEngine_PageLinksCrawlDisallowed(       object sender, PageActionDisallowedArgs e){}
        static void PageProcessorEngine_PageLinksCrawlDisallowedAsync(  object sender, PageActionDisallowedArgs e){}
        static void PageProcessorEngine_PageCrawlDisallowed(            object sender, PageActionDisallowedArgs e){}
        static void PageProcessorEngine_PageCrawlDisallowedAsync(       object sender, PageActionDisallowedArgs e){}
        static void PageProcessorEngine_PageProcessingCompleted(        object sender, PageActionCompletedArgs e){}
        static void PageProcessorEngine_PageProcessingCompletedAsync(   object sender, PageActionCompletedArgs e){}
        

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new Crawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
            //Create a config object manually
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = false;
            config.IsExternalPageLinksCrawlingEnabled = false;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 10;
            config.MaxPagesToCrawl = 10;
            config.MaxPagesToCrawlPerDomain = 0;
            config.MinCrawlDelayPerDomainMilliSeconds = 1000;
            config.UserAgentString = "abot v@ABOTASSEMBLYVERSION@ http://code.google.com/p/abot";

            //Add you own values without modifying Abot's source code.
            //These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
            config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new Crawler(config);
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler(CrawlConfiguration crawlConfig)
        {
            ImplementationOverride implOverride = new ImplementationOverride(crawlConfig);

            //Tell Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is run after the ICrawlDecsionMaker.ShouldCrawlPage method is run.
            implOverride.ShouldCrawlPage = (pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains("ghost"))
                    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                return new CrawlDecision { Allow = true };
            };

            //Tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This is run after the ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            implOverride.ShouldDownloadPageContent = (crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            };

            //Tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This run after the ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            implOverride.ShouldCrawlPageLinks = (crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            };

            return new Crawler(implOverride);
        }

        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInput = "";
            if (args.Length < 1)
            {
                System.Console.WriteLine("Please enter ABSOLUTE url to crawl:");
                userInput = System.Console.ReadLine();
            }
            else
            {
                userInput = args[0];
            }

            if (string.IsNullOrWhiteSpace(userInput))
                throw new ApplicationException("Site url to crawl is as a required parameter");

            return new Uri(userInput);
        }

        private static void PrintDisclaimer()
        {
            PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }

    }
}
