# Abot [![Build Status](https://ci.appveyor.com/api/projects/status/nr84t6dpneg5tmb6?svg=true)](https://ci.appveyor.com/project/sjdirect/abot2) [![NuGet](https://img.shields.io/nuget/v/Abot.svg)](https://www.nuget.org/packages/Abot/)

*Please star this project!!*

###### C# web crawler built for speed and flexibility.

Abot is an open source C# web crawler framework built for speed and flexibility. It takes care of the low level plumbing (multithreading, http requests, scheduling, link parsing, etc..). You just register for events to process the page data. You can also plugin your own implementations of core interfaces to take complete control over the crawl process. Abot Nuget package version >= 2.0 targets Dotnet Standard 2.0 and Abot Nuget package version < 2.0 targets .NET version 4.0 which makes it highly compatible with many .net framework/core implementations.

###### What's So Great About It?
  * Open Source (Free for commercial and personal use)
  * It's fast, really fast!!
  * Easily customizable (Pluggable architecture allows you to decide what gets crawled and how)
  * Heavily unit tested (High code coverage)
  * Very lightweight (not over engineered)
  * No out of process dependencies (no databases, no installed services, etc...)

###### Links of Interest

  * [Ask a question](http://groups.google.com/group/abot-web-crawler), please search for similar questions first!!!
  * [Report a bug](https://github.com/sjdirect/abot/issues)
  * [Learn how you can contribute](https://github.com/sjdirect/abot/wiki/Contribute)
  * [Need expert Abot customization?](https://github.com/sjdirect/abot/wiki/Custom-Development)
  * [Take the usage survey](https://www.surveymonkey.com/s/JS5826F) to help prioritize features/improvements
  * [Consider making a donation](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)

###### Use [AbotX](https://github.com/sjdirect/abotx/blob/master/README.md) for more powerful extensions/wrappers

  * [Crawl multiple sites concurrently](https://github.com/sjdirect/abotx/blob/master/README.md#parallel-crawler-engine)
  * [Execute/Render Javascript](https://github.com/sjdirect/abotx/blob/master/README.md#javascript-rendering)
  * [Avoid getting blocked by sites](https://github.com/sjdirect/abotx/blob/master/README.md#auto-throttling)
  * [Auto Tuning](https://github.com/sjdirect/abotx/blob/master/README.md#auto-tuning)
  * [Auto Throttling](https://github.com/sjdirect/abotx/blob/master/README.md#auto-throttling)
  * [Pause/Resume live crawls](https://github.com/sjdirect/abotx/blob/master/README.md#pause-and-resume)
  * [Simplified pluggability/extensibility](https://github.com/sjdirect/abotx/blob/master/README.md#easy-override)

<br /><br />
<hr />

## Quick Start 

###### Installing Abot
  * Install Abot using [Nuget](https://www.nuget.org/packages/Abot/)

###### Using Abot 
```c#
using System;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Serilog;

namespace TestAbotUse
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Demo starting up!");

            await DemoSimpleCrawler();
            await DemoSinglePageRequest();
        }

        private static async Task DemoSimpleCrawler()
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 10, //Only crawl 10 pages
                MinCrawlDelayPerDomainMilliSeconds = 3000 //Wait this many millisecs between requests
            };
            var crawler = new PoliteWebCrawler(config);

            crawler.PageCrawlCompleted += PageCrawlCompleted;//Several events available...

            var crawlResult = await crawler.CrawlAsync(new Uri("http://!!!!!!!!YOURSITEHERE!!!!!!!!!.com"));
        }

        private static async Task DemoSinglePageRequest()
        {
            var pageRequester = new PageRequester(new CrawlConfiguration(), new WebContentExtractor());

            var crawledPage = await pageRequester.MakeRequestAsync(new Uri("http://google.com"));
            Log.Logger.Information("{result}", new
            {
                url = crawledPage.Uri,
                status = Convert.ToInt32(crawledPage.HttpResponseMessage.StatusCode)
            });
        }

        private static void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            var httpStatus = e.CrawledPage.HttpResponseMessage.StatusCode;
            var rawPageText = e.CrawledPage.Content.Text;
        }
    }
}

```

## Abot Configuration
Abot's Abot2.Poco.CrawlConfiguration class has a ton of configuration options. You can see what effect each config value has on the crawl by looking at the [code comments ](https://github.com/sjdirect/abot/blob/master/Abot2/Poco/CrawlConfiguration.cs).
    
```c#
var crawlConfig = new CrawlConfiguration();
crawlConfig.CrawlTimeoutSeconds = 100;
crawlConfig.MaxConcurrentThreads = 10;
crawlConfig.MaxPagesToCrawl = 1000;
crawlConfig.UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue1", "1111");
crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue2", "2222");
etc...
```

## Abot Events
Register for events and create processing methods
```c#
crawler.PageCrawlStarting += crawler_ProcessPageCrawlStarting;
crawler.PageCrawlCompleted += crawler_ProcessPageCrawlCompleted;
crawler.PageCrawlDisallowed += crawler_PageCrawlDisallowed;
crawler.PageLinksCrawlDisallowed += crawler_PageLinksCrawlDisallowed;
```
```c#
void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
{
	PageToCrawl pageToCrawl = e.PageToCrawl;
	Console.WriteLine($"About to crawl link {pageToCrawl.Uri.AbsoluteUri} which was found on page {pageToCrawl.ParentUri.AbsoluteUri}");
}

void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
{
	CrawledPage crawledPage = e.CrawledPage;

	if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
		Console.WriteLine($"Crawl of page failed {crawledPage.Uri.AbsoluteUri}");
	else
		Console.WriteLine($"Crawl of page succeeded {crawledPage.Uri.AbsoluteUri}");

	if (string.IsNullOrEmpty(crawledPage.Content.Text))
		Console.WriteLine($"Page had no content {crawledPage.Uri.AbsoluteUri}");
	
	var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
}

void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
{
	CrawledPage crawledPage = e.CrawledPage;
	Console.WriteLine($"Did not crawl the links on page {crawledPage.Uri.AbsoluteUri} due to {e.DisallowedReason}");
}

void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
{
	PageToCrawl pageToCrawl = e.PageToCrawl;
	Console.WriteLine($"Did not crawl page {pageToCrawl.Uri.AbsoluteUri} due to {e.DisallowedReason}");
}
```

## Custom objects and the dynamic crawl bag
Add any number of custom objects to the dynamic crawl bag or page bag. These objects will be available in the CrawlContext.CrawlBag object, PageToCrawl.PageBag object or CrawledPage.PageBag object.
```c#
var crawler crawler = new PoliteWebCrawler();
crawler.CrawlBag.MyFoo1 = new Foo();
crawler.CrawlBag.MyFoo2 = new Foo();
crawler.PageCrawlStarting += crawler_ProcessPageCrawlStarting;
...
```
```c#
void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
{
    //Get your Foo instances from the CrawlContext object
    var foo1 = e.CrawlConext.CrawlBag.MyFoo1;
    var foo2 = e.CrawlConext.CrawlBag.MyFoo2;

    //Also add a dynamic value to the PageToCrawl or CrawledPage
    e.PageToCrawl.PageBag.Bar = new Bar();
}
```

## Cancellation
```c#
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

var crawler = new PoliteWebCrawler();
var result = await crawler.CrawlAsync(new Uri("addurihere"), cancellationTokenSource);
```

<br /><br /><br />
<hr />

## Customizing Crawl Behavior

Abot was designed to be as pluggable as possible. This allows you to easily alter the way it works to suite your needs.

The easiest way to change Abot's behavior for common features is to change the config values that control them. See the [Quick Start](#quick-start) page for examples on the different ways Abot can be configured.

#### CrawlDecision Callbacks/Delegates
Sometimes you don't want to create a class and go through the ceremony of extending a base class or implementing the interface directly. For all you lazy developers out there Abot provides a shorthand method to easily add your custom crawl decision logic. NOTE: The ICrawlDecisionMaker's corresponding method is called first and if it does not "allow" a decision, these callbacks will not be called.

```c#
var crawler = new PoliteWebCrawler();

crawler.ShouldCrawlPageDecisionMaker((pageToCrawl, crawlContext) => 
{
	var decision = new CrawlDecision{ Allow = true };
	if(pageToCrawl.Uri.Authority == "google.com")
		return new CrawlDecision{ Allow = false, Reason = "Dont want to crawl google pages" };
	
	return decision;
});

crawler.ShouldDownloadPageContentDecisionMaker((crawledPage, crawlContext) =>
{
	var decision = new CrawlDecision{ Allow = true };
	if (!crawledPage.Uri.AbsoluteUri.Contains(".com"))
		return new CrawlDecision { Allow = false, Reason = "Only download raw page content for .com tlds" };

	return decision;
});

crawler.ShouldCrawlPageLinksDecisionMaker((crawledPage, crawlContext) =>
{
	var decision = new CrawlDecision{ Allow = true };
	if (crawledPage.Content.Bytes.Length < 100)
		return new CrawlDecision { Allow = false, Reason = "Just crawl links in pages that have at least 100 bytes" };

	return decision;
});
```

#### Custom Implementations
PoliteWebCrawler is the master of orchestrating the crawl. Its job is to coordinate all the utility classes to "crawl" a site. PoliteWebCrawler accepts an alternate implementation for all its dependencies through its constructor.
 
```c#
var crawler = new PoliteWebCrawler(
    	new CrawlConfiguration(),
	new YourCrawlDecisionMaker(),
	new YourThreadMgr(), 
	new YourScheduler(), 
	new YourPageRequester(), 
	new YourHyperLinkParser(), 
	new YourMemoryManager(), 
    	new YourDomainRateLimiter,
	new YourRobotsDotTextFinder());
```

Passing null for any implementation will use the default. The example below will use your custom implementation for the IPageRequester and IHyperLinkParser but will use the default for all others.

```c#
var crawler = new PoliteWebCrawler(
	null, 
	null, 
    	null,
    	null,
	new YourPageRequester(), 
	new YourHyperLinkParser(), 
	null,
    	null, 
	null);
```

The following are explanations of each interface that PoliteWebCrawler relies on to do the real work.

###### ICrawlDecisionMaker
The callback/delegate shortcuts are great to add a small amount of logic but if you are doing anything more heavy you will want to pass in your custom implementation of ICrawlDecisionMaker. The crawler calls this implementation to see whether a page should be crawled, whether the page's content should be downloaded and whether a crawled page's links should be crawled.

[CrawlDecisionMaker.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Core/CrawlDecisionMaker.cs) is the default ICrawlDecisionMaker used by Abot. This class takes care of common checks like making sure the config value MaxPagesToCrawl is not exceeded. Most users will only need to create a class that extends CrawlDecision maker and just add their custom logic. However, you are completely free to create a class that implements ICrawlDecisionMaker and pass it into PoliteWebCrawlers constructor.

```c#
/// <summary>
/// Determines what pages should be crawled, whether the raw content should be downloaded and if the links on a page should be crawled
/// </summary>
public interface ICrawlDecisionMaker
{
	/// <summary>
	/// Decides whether the page should be crawled
	/// </summary>
	CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext);

	/// <summary>
	/// Decides whether the page's links should be crawled
	/// </summary>
	CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext);

	/// <summary>
	/// Decides whether the page's content should be dowloaded
	/// </summary>
	CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext);
}
```


###### IThreadManager
The IThreadManager interface deals with the multithreading details. It is used by the crawler to manage concurrent http requests. 

[TaskThreadManager.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Util/TaskThreadManager.cs) is the default IThreadManager used by Abot. 


```c#
/// <summary>
/// Handles the multithreading implementation details
/// </summary>
public interface IThreadManager : IDisposable
{
	/// <summary>
	/// Max number of threads to use.
	/// </summary>
	int MaxThreads { get; }

	/// <summary>
	/// Will perform the action asynchrously on a seperate thread
	/// </summary>
	/// <param name="action">The action to perform</param>
	void DoWork(Action action);

	/// <summary>
	/// Whether there are running threads
	/// </summary>
	bool HasRunningThreads();

	/// <summary>
	/// Abort all running threads
	/// </summary>
	void AbortAll();
}
```


###### IScheduler
The IScheduler interface deals with managing what pages need to be crawled. The crawler gives the links it finds to and gets the pages to crawl from the IScheduler implementation. A common use cases for writing your own implementation might be to distribute crawls across multiple machines which could be managed by a DistributedScheduler.

[Scheduler.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Core/Scheduler.cs) is the default IScheduler used by the crawler and by default is constructed with in memory collection to determine what pages have been crawled and which need to be crawled. 

```c#
/// <summary>
/// Handles managing the priority of what pages need to be crawled
/// </summary>
public interface IScheduler
{
	/// <summary>
	/// Count of remaining items that are currently scheduled
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Schedules the param to be crawled
	/// </summary>
	void Add(PageToCrawl page);

	/// <summary>
	/// Schedules the param to be crawled
	/// </summary>
	void Add(IEnumerable<PageToCrawl> pages);

	/// <summary>
	/// Gets the next page to crawl
	/// </summary>
	PageToCrawl GetNext();

	/// <summary>
	/// Clear all currently scheduled pages
	/// </summary>
	void Clear();
}
```


###### IPageRequester
The IPageRequester interface deals with making the raw http requests.

[PageRequester.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Core/PageRequester.cs) is the default IPageRequester used by the crawler. 

```c#
public interface IPageRequester
{
	/// <summary>
	/// Make an http web request to the url and download its content
	/// </summary>
	CrawledPage MakeRequest(Uri uri);

	/// <summary>
	/// Make an http web request to the url and download its content based on the param func decision
	/// </summary>
	CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
}
```

###### IHyperLinkParser
The IHyperLinkParser interface deals with parsing the links out of raw html.

[HapHyperlinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Core/HapHyperLinkParser.cs) is the default IHyperLinkParser used by the crawler. It uses the well known parsing library [Html Agility Pack](http://htmlagilitypack.codeplex.com/). There is also an alternative implementation [AngleSharpHyperLinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Core/AngleSharpHyperLinkParser.cs) which uses [AngleSharp](https://github.com/AngleSharp/AngleSharp) to do the parsing. AngleSharp uses a css style selector like jquery but all in c#. 

```c#
/// <summary>
/// Handles parsing hyperlikns out of the raw html
/// </summary>
public interface IHyperLinkParser
{
	/// <summary>
	/// Parses html to extract hyperlinks, converts each into an absolute url
	/// </summary>
	IEnumerable<Uri> GetLinks(CrawledPage crawledPage);
}
```

###### IMemoryManager
The IMemoryManager handles memory monitoring. This feature is still experimental and could be removed in a future release if found to be unreliable. 

[MemoryManager.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Util/MemoryManager.cs) is the default implementation used by the crawler. 

```c#
/// <summary>
/// Handles memory monitoring/usage
/// </summary>
public interface IMemoryManager : IMemoryMonitor, IDisposable
{
	/// <summary>
	/// Whether the current process that is hosting this instance is allocated/using above the param value of memory in mb
	/// </summary>
	bool IsCurrentUsageAbove(int sizeInMb);

	/// <summary>
	/// Whether there is at least the param value of available memory in mb
	/// </summary>
	bool IsSpaceAvailable(int sizeInMb);
}
```

###### IDomainRateLimiter
The IDomainRateLimiter handles domain rate limiting. It will handle determining how much time needs to elapse before it is ok to make another http request to the domain.

[DomainRateLimiter.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core2/DomainRateLimiter.cs) is the default implementation used by the crawler. 

```c#
/// <summary>
/// Rate limits or throttles on a per domain basis
/// </summary>
public interface IDomainRateLimiter
{
	/// <summary>
	/// If the domain of the param has been flagged for rate limiting, it will be rate limited according to the configured minimum crawl delay
	/// </summary>
	void RateLimit(Uri uri);

	/// <summary>
	/// Add a domain entry so that domain may be rate limited according the the param minumum crawl delay
	/// </summary>
	void AddDomain(Uri uri, long minCrawlDelayInMillisecs);
}
```


###### IRobotsDotTextFinder
The IRobotsDotTextFinder is responsible for retrieving the robots.txt file for every domain (if isRespectRobotsDotTextEnabled="true") and building the robots.txt abstraction which implements the IRobotsDotText interface. 

[RobotsDotTextFinder.cs](https://github.com/sjdirect/abot/blob/master/Abot2/Core/RobotsDotTextFinder.cs) is the default implementation used by the crawler. 

```c#
/// <summary>
/// Finds and builds the robots.txt file abstraction
/// </summary>
public interface IRobotsDotTextFinder
{
	/// <summary>
	/// Finds the robots.txt file using the rootUri. 
        /// 
	IRobotsDotText Find(Uri rootUri);
}
```

<br /><br /><br />
<hr />
