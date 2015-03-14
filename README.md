#abot [![Build Status](https://ci.appveyor.com/api/projects/status/b1ukruawvu6uujn0?svg=true)](https://ci.appveyor.com/project/sjdirect/abot)

######C# web crawler build for speed and flexibility.

Abot is an open source C# web crawler built for speed and flexibility. It takes care of the low level plumbing (multithreading, http requests, scheduling, link parsing, etc..). You just register for events to process the page data. You can also plugin your own implementations of core interfaces to take complete control over the crawl process.

Abot targets .NET version 4.0.

  * [Ask questions and search for answers on the Community Forum](http://groups.google.com/group/abot-web-crawler)<br />
  * [Report Bugs or Suggest Features](https://github.com/sjdirect/abot/issues)
  * [Consider making a donation](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)

######What's So Great About It?
  * Open Source (Free for commercial and personal use)
  * Speed
  * Every part of the architecture is pluggable
  * Heavily unit tested (High code coverage)
  * Very lightweight (not over engineered)
  * No database required
  * Easy to customize (When to crawl a page, when not to crawl, when to crawl page's links, etc..)
  * Runs on Mono

###Quick Start

######Installing Abot
  * Install Abot using [Nuget](https://www.nuget.org/packages/Abot/)
  * If you prefer to build from source yourself see the [Working With The Source Code section](#SourceCode) below

######Using Abot
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
    <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net"/>
    <section name="abot" type="Abot.Core.AbotConfigurationSectionHandler, Abot"/>
  </configSections>
  <runtime>
    <!-- Experiment with these to see if it helps your memory usage, USE ONLY ONE OF THE FOLLOWING -->
    <!--<gcServer enabled="true"/>-->
    <!--<gcConcurrent enabled="true"/>-->
  </runtime>
  <log4net>
    <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="[%date] [%thread] [%-5level] - %message%newline"/>
      </layout>
    </appender>
    <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
      <file value="abotlog.txt"/>
      <appendToFile value="true"/>
      <rollingStyle value="Size"/>
      <maxSizeRollBackups value="10"/>
      <maximumFileSize value="10240KB"/>
      <staticLogFileName value="true"/>
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="[%date] [%-3thread] [%-5level] - %message%newline"/>
      </layout>
    </appender>
    <root>
      <level value="INFO"/>
      <appender-ref ref="ConsoleAppender"/>
      <appender-ref ref="RollingFileAppender"/>
    </root>
  </log4net>

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
      minAvailableMemoryRequiredInMb="0"
      maxMemoryUsageInMb="0"
      maxMemoryUsageCacheTimeInSeconds="0"
      maxCrawlDepth="1000"
      isForcedLinkParsingEnabled="false"
      maxRetryCount="0"
      minRetryDelayInMilliseconds="0"
      />
    <politeness 
      isRespectRobotsDotTextEnabled="false"
      isRespectMetaRobotsNoFollowEnabled="false"
      isRespectAnchorRelNoFollowEnabled="false"
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
Option 2: Create an instance of the Abot.Poco.CrawlConfiguration class manually. This approach ignores the app.config values completely. 
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
CrawlResult result = crawler.Crawl(new Uri("http://localhost:1111/"));

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


###Logging (Optional)
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

###Customizing Crawl Behavior

Abot was designed to be as pluggable as possible. This allows you to easily alter the way it works to suite your needs.

The easiest way to change Abot's behavior for common features is to change the config values that control them. See the [Quick Start](#quickstart) page for examples on the different ways Abot can be configured.

######CrawlDecision Callbacks
Sometimes you don't want to create a class and go through the ceremony of extending a base class or implementing the interface directly. For all you lazy developers out there Abot provides a shorthand method to easily add your custom crawl decision logic. NOTE: The ICrawlDecisionMaker's corresponding method is called first and if it does not "allow" a decision, these callbacks will not be called.

```c#
PoliteWebCrawler crawler = new PoliteWebCrawler();

crawler.ShouldCrawlPage((pageToCrawl, crawlContext) => 
{
	CrawlDecision decision = new CrawlDecision();
	if(pageToCrawl.Uri.Authority == "google.com")
		return new CrawlDecision{ Allow = false, Reason = "Dont want to crawl google pages" };
	
	return decision;
});

crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
{
	CrawlDecision decision = new CrawlDecision();
	if (!crawledPage.Uri.AbsoluteUri.Contains(".com"))
		return new CrawlDecision { Allow = false, Reason = "Only download raw page content for .com tlds" };

	return decision;
});

crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
{
	CrawlDecision decision = new CrawlDecision();
	if (crawledPage.PageSizeInBytes < 100)
		return new CrawlDecision { Allow = false, Reason = "Just crawl links in pages that have at least 100 bytes" };

	return decision;
});
```


######Custom Implementations
PoliteWebCrawler is the master of orchestrating the crawl. It's job is to coordinate all the utility classes to "crawl" a site. PoliteWebCrawler accepts an alternate implementation for all its dependencies through it's constructor.
 
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

######ICrawlDecisionMaker
The delegate shortcuts are great to add a small amount of logic but if you are doing anything more heavy you will want to pass in your custom implementation of ICrawlDecisionMaker. The crawler calls this implementation to see whether a page should be crawled, whether the page's content should be downloaded and whether a crawled page's links should be crawled.

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


######IThreadManager
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


######IScheduler
The IScheduler interface deals with managing what pages need to be crawled. The crawler gives the links it finds to and gets the pages to crawl from the IScheduler implementation. A common use cases for writing your own implementation might be to distribute crawls across multiple machines which could be managed by a DistributedScheduler.

[Scheduler.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/Scheduler.cs] is the default IScheduler used by the crawler and by default is constructed with in memory collection to determine what pages have been crawled and which need to be crawled. 

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


######IPageRequester
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

######IHyperLinkParser
The IHyperLinkParser interface deals with parsing the links out of raw html.

[HapHyperlinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/HapHyperLinkParser.cs) is the default IHyperLinkParser used by the crawler. It uses the well known parsing library [Html Agility Pack](http://htmlagilitypack.codeplex.com/). There is also an alternative implementation [CsQueryHyperLinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/CsQueryHyperLinkParser.cs) which uses [CsQuery](https://github.com/jamietre/CsQuery) to do the parsing. CsQuery uses a css style selector like jquery but all in c#. 

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

######IMemoryManager
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

######IDomainRateLimiter
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


######IRobotsDotTextFinder
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
