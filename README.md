# Abot [![Build Status](https://ci.appveyor.com/api/projects/status/b1ukruawvu6uujn0/branch/master?svg=true)](https://ci.appveyor.com/project/sjdirect/abot) [![NuGet](https://img.shields.io/nuget/v/Abot.svg)](https://www.nuget.org/packages/Abot/)

*Please star this project!!* Contact me with exciting opportunities!!

###### C# web crawler built for speed and flexibility.

Abot is an open source C# web crawler built for speed and flexibility. It takes care of the low level plumbing (multithreading, http requests, scheduling, link parsing, etc..). You just register for events to process the page data. You can also plugin your own implementations of core interfaces to take complete control over the crawl process. Abot targets .NET version 4.0 which makes it highly compatible with many .net framework implementations.

###### What's So Great About It?
  * Open Source (Free for commercial and personal use)
  * It's fast!!
  * Easily customizable (Pluggable architecture allows you to decide what gets crawled and how)
  * Heavily unit tested (High code coverage)
  * Very lightweight (not over engineered)
  * No out of process dependencies (database, installed services, etc...)

###### Links of Interest

  * [No more free support](https://github.com/sjdirect/abot/wiki/Support), sorry guys/gals :(
  * [Ask a question](http://groups.google.com/group/abot-web-crawler), please search for similar questions first!!!
  * [Report a bug](https://github.com/sjdirect/abot/issues)
  * [Learn how you can contribute](https://github.com/sjdirect/abot/wiki/Contribute)
  * [Need expert Abot customization?](https://github.com/sjdirect/abot/wiki/Custom-Development)
  * [Take the usage survey](https://www.surveymonkey.com/s/JS5826F) to help prioritize features/improvements
  * [Consider making a donation](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)
  * [Unofficial Chinese Documentation](https://github.com/zixiliuyue/abot)

###### Use [AbotX](http://abotx.org) for powerful extensions/wrappers

  * [Crawl multiple sites concurrently](http://abotx.org/Learn/ParallelCrawlerEngine)
  * [Execute/Render Javascript](http://abotx.org/Learn/JavascriptRendering)
  * [Avoid getting blocked by sites](http://abotx.org/Learn/AutoThrottling)
  * [Auto Tuning](http://abotx.org/Learn/AutoTuning)
  * [Auto Throttling](http://abotx.org/Learn/AutoThrottling)
  * [Pause/Resume live crawls](http://abotx.org/Learn/CrawlerX#crawlerx-pause-resume)
  * [Simplified pluggability/extensibility](https://abotx.org/Learn/CrawlerX#easy-override)

<br /><br />
<hr />

## Quick Start 

###### Installing Abot
  * Install Abot using [Nuget](https://www.nuget.org/packages/Abot/)
  * If you prefer to build from source yourself see the [Working With The Source Code section](#working-with-the-source-code) below

###### Using Abot
1: Add the following using statements to the host class... 
```c#
using Abot.Crawler;
using Abot.Poco;
```
2: Configure Abot using any of the options below. You can see what effect each config value has on the crawl by looking at the [code comments ](https://github.com/sjdirect/abot/blob/master/Abot/Poco/CrawlConfiguration.cs).
    
**Option 1:** Add the following to the app.config or web.config file of the assembly using the library. Nuget will NOT add this for you. *NOTE: The gcServer or gcConcurrent entry may help memory usage in your specific use of abot.*
```xml
<configuration>
  <configSections>
    <section name="abot" type="Abot.Core.AbotConfigurationSectionHandler, Abot"/>
  </configSections>
  
  <runtime>
    <!-- Experiment with these to see if it helps your memory usage, USE ONLY ONE OF THE FOLLOWING -->
    <!--<gcServer enabled="true"/>-->
    <!--<gcConcurrent enabled="true"/>-->
  </runtime>

  <abot>
    <crawlBehavior 
      maxConcurrentThreads="10" 
      maxPagesToCrawl="1000" 
      maxPagesToCrawlPerDomain="0" 
      maxPageSizeInBytes="0"
      userAgentString="Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko" 
      crawlTimeoutSeconds="0" 
      downloadableContentTypes="text/html, text/plain" 
      isUriRecrawlingEnabled="false" 
      isExternalPageCrawlingEnabled="false" 
      isExternalPageLinksCrawlingEnabled="false"
      httpServicePointConnectionLimit="200"  
      httpRequestTimeoutInSeconds="15" 
      httpRequestMaxAutoRedirects="7" 
      isHttpRequestAutoRedirectsEnabled="true" 
      isHttpRequestAutomaticDecompressionEnabled="false"
      isSendingCookiesEnabled="false"
      isSslCertificateValidationEnabled="false"
      isRespectUrlNamedAnchorOrHashbangEnabled="false"
      minAvailableMemoryRequiredInMb="0"
      maxMemoryUsageInMb="0"
      maxMemoryUsageCacheTimeInSeconds="0"
      maxCrawlDepth="1000"
	  maxLinksPerPage="1000"
      isForcedLinkParsingEnabled="false"
      maxRetryCount="0"
      minRetryDelayInMilliseconds="0"
      />
    <authorization
      isAlwaysLogin="false"
      loginUser=""
      loginPassword="" />	  
    <politeness 
      isRespectRobotsDotTextEnabled="false"
      isRespectMetaRobotsNoFollowEnabled="false"
	  isRespectHttpXRobotsTagHeaderNoFollowEnabled="false"
      isRespectAnchorRelNoFollowEnabled="false"
      isIgnoreRobotsDotTextIfRootDisallowedEnabled="false"
      robotsDotTextUserAgentString="abot"
      maxRobotsDotTextCrawlDelayInSeconds="5" 
      minCrawlDelayPerDomainMilliSeconds="0"/>
    <extensionValues>
      <add key="key1" value="value1"/>
      <add key="key2" value="value2"/>
    </extensionValues>
  </abot>  
</configuration>    
```
**Option 2:** Create an instance of the Abot.Poco.CrawlConfiguration class manually. This approach ignores the app.config values completely. 
```c#
CrawlConfiguration crawlConfig = new CrawlConfiguration();
crawlConfig.CrawlTimeoutSeconds = 100;
crawlConfig.MaxConcurrentThreads = 10;
crawlConfig.MaxPagesToCrawl = 1000;
crawlConfig.UserAgentString = "abot v1.0 http://code.google.com/p/abot";
crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue1", "1111");
crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue2", "2222");
etc...
```
**Option 3:** Both!! Load from app.config then tweek   
```c#
CrawlConfiguration crawlConfig = AbotConfigurationSectionHandler.LoadFromXml().Convert();
crawlConfig.MaxConcurrentThreads = 5;//this overrides the config value
etc...
```

3: Create an instance of Abot.Crawler.PoliteWebCrawler
```c#
//Will use app.config for configuration
PoliteWebCrawler crawler = new PoliteWebCrawler();
```
```c#
//Will use the manually created crawlConfig object created above
PoliteWebCrawler crawler = new PoliteWebCrawler(crawlConfig, null, null, null, null, null, null, null, null);
```
4: Register for events and create processing methods (both synchronous and asynchronous versions available)
```c#
crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;
```
```c#
void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
{
	PageToCrawl pageToCrawl = e.PageToCrawl;
	Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri,   pageToCrawl.ParentUri.AbsoluteUri);
}

void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
{
	CrawledPage crawledPage = e.CrawledPage;

	if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
		Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
	else
		Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

	if (string.IsNullOrEmpty(crawledPage.Content.Text))
		Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);
	
	var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser
	var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
}

void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
{
	CrawledPage crawledPage = e.CrawledPage;
	Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);
}

void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
{
	PageToCrawl pageToCrawl = e.PageToCrawl;
	Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
}
```
5: (Optional) Add any number of custom objects to the dynamic crawl bag or page bag. These objects will be available in the CrawlContext.CrawlBag object, PageToCrawl.PageBag object or CrawledPage.PageBag object.
```c#
PoliteWebCrawler crawler = new PoliteWebCrawler();
crawler.CrawlBag.MyFoo1 = new Foo();
crawler.CrawlBag.MyFoo2 = new Foo();
crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
...
```
```c#
void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
{
    //Get your Foo instances from the CrawlContext object
	CrawlContext context = e.CrawlContext;
    context.CrawlBag.MyFoo1.Bar();
    context.CrawlBag.MyFoo2.Bar();

    //Also add a dynamic value to the PageToCrawl or CrawledPage
    e.PageToCrawl.PageBag.Bar = new Bar();
}
```
6: Run the crawl
```c#
CrawlResult result = crawler.Crawl(new Uri("http://localhost:1111/")); //This is synchronous, it will not go to the next line until the crawl has completed

if (result.ErrorOccurred)
	Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
else
	Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
```
OR run the crawl with a cancellation token to stop the crawl early
```c#
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

PoliteWebCrawler crawler = new PoliteWebCrawler();
CrawlResult result = crawler.Crawl(new Uri("addurihere"), cancellationTokenSource);
```

<br /><br /><br />
<hr />

## Logging (Optional)
Abot uses Log4Net to log messages. These log statements are a great way to see whats going on during a crawl. However, if you dont want to use log4net you can skip this section. 

Below is an example log4net configuration. Read more abot log4net at [their website](http://logging.apache.org/log4net/release/manual/introduction.html)

Add using statement for log4net.
```c#
using log4net.Config;
```

Be sure to call the following method to tell log4net to read in the config file. This call must happen before Abot's Crawl(Uri) method, otherwise you wont see any output. This is usually called in the beginning of a console app or service or the global.asax of a web app.
```c#
XmlConfigurator.Configure();
```

The following configuration data should be added to the app.config file of the application that will be running Abot.
```xml
  <configSections>
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
  </configSections>
  
  <log4net>
    <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="[%date] [%thread] [%-5level] - %message - [%logger]%newline"/>
      </layout>
    </appender>
    <appender name="AbotAppender" type="log4net.Appender.RollingFileAppender">
      <file value="abotlog.txt"/>
      <appendToFile value="true"/>
      <rollingStyle value="Size"/>
      <maxSizeRollBackups value="10"/>
      <maximumFileSize value="10240KB"/>
      <staticLogFileName value="true"/>
      <preserveLogFileNameExtension value="true" />
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="[%date] [%thread] [%-5level] - %message - [%logger]%newline"/>
      </layout>
    </appender>
    <logger name="AbotLogger">
      <level value="INFO"/>
      <appender-ref ref="ConsoleAppender"/>
      <appender-ref ref="AbotAppender"/>
    </logger>
  </log4net>
```

<br /><br /><br />
<hr />

## Customizing Crawl Behavior

Abot was designed to be as pluggable as possible. This allows you to easily alter the way it works to suite your needs.

The easiest way to change Abot's behavior for common features is to change the config values that control them. See the [Quick Start](#quick-start) page for examples on the different ways Abot can be configured.

#### CrawlDecision Callbacks/Delegates
Sometimes you don't want to create a class and go through the ceremony of extending a base class or implementing the interface directly. For all you lazy developers out there Abot provides a shorthand method to easily add your custom crawl decision logic. NOTE: The ICrawlDecisionMaker's corresponding method is called first and if it does not "allow" a decision, these callbacks will not be called.

```c#
PoliteWebCrawler crawler = new PoliteWebCrawler();

crawler.ShouldCrawlPage((pageToCrawl, crawlContext) => 
{
	CrawlDecision decision = new CrawlDecision{ Allow = true };
	if(pageToCrawl.Uri.Authority == "google.com")
		return new CrawlDecision{ Allow = false, Reason = "Dont want to crawl google pages" };
	
	return decision;
});

crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
{
	CrawlDecision decision = new CrawlDecision{ Allow = true };
	if (!crawledPage.Uri.AbsoluteUri.Contains(".com"))
		return new CrawlDecision { Allow = false, Reason = "Only download raw page content for .com tlds" };

	return decision;
});

crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
{
	CrawlDecision decision = new CrawlDecision{ Allow = true };
	if (crawledPage.Content.Bytes.Length < 100)
		return new CrawlDecision { Allow = false, Reason = "Just crawl links in pages that have at least 100 bytes" };

	return decision;
});
```

#### Custom Implementations
PoliteWebCrawler is the master of orchestrating the crawl. Its job is to coordinate all the utility classes to "crawl" a site. PoliteWebCrawler accepts an alternate implementation for all its dependencies through its constructor.
 
```c#
PoliteWebCrawler crawler = new PoliteWebCrawler(
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
PoliteWebCrawler crawler = new PoliteWebCrawler(
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

[CrawlDecisionMaker.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/CrawlDecisionMaker.cs) is the default ICrawlDecisionMaker used by Abot. This class takes care of common checks like making sure the config value MaxPagesToCrawl is not exceeded. Most users will only need to create a class that extends CrawlDecision maker and just add their custom logic. However, you are completely free to create a class that implements ICrawlDecisionMaker and pass it into PoliteWebCrawlers constructor.

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

[TaskThreadManager.cs](https://github.com/sjdirect/abot/blob/master/Abot/Util/TaskThreadManager.cs) is the default IThreadManager used by Abot. 


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

[Scheduler.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/Scheduler.cs) is the default IScheduler used by the crawler and by default is constructed with in memory collection to determine what pages have been crawled and which need to be crawled. 

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

[PageRequester.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/PageRequester.cs) is the default IPageRequester used by the crawler. 

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

[HapHyperlinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/HapHyperLinkParser.cs) is the default IHyperLinkParser used by the crawler. It uses the well known parsing library [Html Agility Pack](http://htmlagilitypack.codeplex.com/). There is also an alternative implementation [AngleSharpHyperLinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/AngleSharpHyperLinkParser.cs) which uses [AngleSharp](https://github.com/AngleSharp/AngleSharp) to do the parsing. AngleSharp uses a css style selector like jquery but all in c#. 

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

[MemoryManager.cs](https://github.com/sjdirect/abot/blob/master/Abot/Util/MemoryManager.cs) is the default implementation used by the crawler. 

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

[DomainRateLimiter.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/DomainRateLimiter.cs) is the default implementation used by the crawler. 

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

[RobotsDotTextFinder.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/RobotsDotTextFinder.cs) is the default implementation used by the crawler. 

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

## Working With The Source Code
The most common way to customize crawl behavior is by extending classes and overriding methods. You can also create a custom implementation of a core interface. All this can be done outside of Abot's source code. 

However, if the changes that you are going to make are out of the ordinary or you want to contribute a bug fix or feature then you will want to work directly with Abot's source code. Below you will find what you need to get the solution building/running on your local machine.

###### Your First Build
1: Clone the latest using the following commands<br /><br />
  *git clone git@github.com:sjdirect/abot.git*<br /><br />
2: Open the Abot.sln file in Visual Studio (all dev done in vs 2013 premium)<br />
3: Build the solution normally<br />

###### External Tools Needed
NUnit Test Runner: The unit tests for Abot are using NUnit which is not supported right out of the box in visual studio. You must either install a NUnit test adapter or a product like TestDriven or Resharper. Download the [NUnit test adapter](http://visualstudiogallery.msdn.microsoft.com/6ab922d0-21c0-4f06-ab5f-4ecd1fe7175d) or install it through visual studio extension manager.

###### Solution Project/Assembly Overview
* **Abot:** Main library for all crawling and utility code.<br />
* **Abot.Demo:** Simple console app that demonstrates how to use abot.<br />
* **Abot.SiteSimulator:** An asp.net mvc application that can simulate any number of pages and several http responses that are encountered during a crawl. This site is used to produce a predictable site crawl for abot.
Both Abot.Tests.Unit and Abot.Tests.Integration make calls to this site. However a sample of those calls were saved/stored in a fiddler session and are automatically replayed by FiddlerCore everytime the unit or integration tests are run. <br />
* **Abot.Tests.Unit:** Unit tests for all Abot assemblies. Abot.SiteSimulator site must be running for tests to pass since mocking http web requests is more trouble then its worth.<br />
* **Abot.Tests.Integration:** Tests the end to end crawl behavior. These are real crawls, no mocks/stubs/etc.. Abot.SiteSimulator site must be running for tests to pass.<br />

###### How to run Abot.Demo
The demo project has a few config values set that greatly limit Abot's speed.  This is to make sure you don't get banned by your isp provider or get blocked by the sites you are crawling. These setting are..

```xml
<abot>
    <crawlBehavior 
      ...(excluded)
      maxConcurrentThreads="1" 
      maxPagesToCrawl="10" 
      ...(excluded)
      />
    <politeness 
      ...(excluded)
      minCrawlDelayPerDomainMilliSeconds="1000"
      ...(excluded)
      />
  </abot>  
```

This will tell Abot to use 1 thread, to only crawl 10 pages and that it must wait 1 second between each http request. If you want to get a feel for the real speed of Abot then change those settings to the following...

```xml
<abot>
    <crawlBehavior 
      ...(excluded)
      maxConcurrentThreads="10" 
      maxPagesToCrawl="10000" 
      ...(excluded)
      />
    <politeness 
      ...(excluded)
      minCrawlDelayPerDomainMilliSeconds="0"
      ...(excluded)
      />
  </abot>  
```

This will tell Abot to use 10 threads, to crawl up to 10,000 pages and that it should NOT wait in between requests. Abot will be requesting and processing up to 10 pages at a time concurrently. 

1: Right click on the Abot.Demo project and set it as the "startup project"<br />
2: Then hit ctrl + F5 to see the console app run.<br />
3: When prompted for a url enter whatever site you want to crawl (must begin with "http://" or "https://")<br />
4: Press enter<br />
5: View the Abot.Demo/bin/debug/abotlog.txt file for all the output.<br />

###### How To Run Demo Against Abot.SiteSimulator

If you would rather test your crawls on a test site then I would suggest you use the Abot.SiteSimulator mvc project. This site is hosted on your machine and will not generate any http traffic beyond your local network. This allows you to crawl as aggressively as you would like without of fear of isp issues. This site also has links that purposefully return http 200, 301-302, 403-404 and 500 responses to simulate a wide range of what can be encountered while crawling the web. To use the Abot.SiteSimulator project do the following...

1: Right click on the Abot.SiteSimulator project and set it as the "startup project".<br /> 
2: Then hit ctrl + F5 to run it, You should see a simple webpage with a few links on http://localhost:1111/<br />
3: Right click on the Abot.Demo project and set it as the "startup project". <br />
4: Then hit ctrl + F5 to see the console app run.<br />
5: When prompted for a url enter: http://localhost:1111/<br />
6: Press enter<br />
7: View the Abot.Demo/bin/debug/abotlog.txt file for all the output.<br />

Now the Abot.Demo console application will be crawling the Abot.SiteSimulator test site that is running locally on your machine. This is the best way to develop Abot without being banned by your ip.

###### How to run Abot.Tests.Unit
1: Verify "External Tools" defined above are installed and working<br />
2: Run Abot.Tests.Unit tests using whatever test runner you like (Visual Studio test runner, Testdriven.net or Resharper).<br />

###### How to run Abot.Tests.Integration
1: Verify "External Tools" defined above are installed and working<br />
2: Run Abot.Tests.Integration using whatever test runner you like (Visual Studio test runner, Testdriven.net or Resharper).<br />
3: View the file output at Abot.Tests.Integration/bin/debug/abotlog.txt file for all the output.<br />

###### Fiddler Core
Just a note that Fiddler.Core is started and stopped during unit and integration tests. This allows replaying predictable http requests. Read more about [Fiddler Core here](http://www.telerik.com/fiddler/fiddlercore)
