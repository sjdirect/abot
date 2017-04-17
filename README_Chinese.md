#Abot [![Build Status](https://ci.appveyor.com/api/projects/status/b1ukruawvu6uujn0?svg=true)](https://ci.appveyor.com/project/sjdirect/abot)

*Please star this project!!* Contact me with exciting opportunities!!

*开始这个项目!!* 如果可以的话联系我 一起开始这个令人激动的时刻!!

######C# web crawler built for speed and flexibility.

######C# 构建的小巧和灵活的web爬虫框 架.  

Abot is an open source C# web crawler built for speed and flexibility. It takes care of the low level plumbing (multithreading, http requests, scheduling, link parsing, etc..). You just register for events to process the page data. You can also plugin your own implementations of core interfaces to take complete control over the crawl process. Abot targets .NET version 4.0. 

Abot是一个开源的C#编写的小巧灵活的web爬虫框架。他帮你处理了底层的工作(多线程，http请求，调度，链接解析 等)。你只需要在进程中注册事件处理数据。当然了你也可以在代码的接口中插入你自己的爬虫控制代码。Abot面向.NET 4.0平台。

######What's So Great About It?
######Abot 有什么优势?
  * Open Source (Free for commercial and personal use)
  * 开源（供商业和个人免费试用）
  * It's fast!!
  * 它是快速高效的
  * Easily customizable (Pluggable architecture allows you to decide what gets crawled and how)
  * 容易定制（插件式开发让你决定抓取什么怎样抓取）
  * Heavily unit tested (High code coverage)
  * 重视单元测试（高质量的抓取）
  * Very lightweight (not over engineered)
  * 轻量级框架（非工程级代码）
  * No out of process dependencies (database, installed services, etc...)
  * 没有其他的依赖（如数据库，服务安装等）
  * Runs on Mono
  * 运行在Mono上

######Links of Interest
######有关的网络连接

  * [Ask a question](http://groups.google.com/group/abot-web-crawler)
  * [Report a bug or suggest a feature](https://github.com/sjdirect/abot/issues)
  * [Learn how you can contribute](https://github.com/sjdirect/abot/wiki/Contribute)
  * [Need expert Abot customization?](https://github.com/sjdirect/abot/wiki/Custom-Development)
  * [Take the usage survey](https://www.surveymonkey.com/s/JS5826F) to help prioritize features/improvements
  * [Consider making a donation](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)
  * [提问一个问题](http://groups.google.com/group/abot-web-crawler)
  * [报告一个bug或者提供支持](https://github.com/sjdirect/abot/issues)
  * [学习怎么贡献例子](https://github.com/sjdirect/abot/wiki/Contribute)
  * [个性化定制?](https://github.com/sjdirect/abot/wiki/Custom-Development)
  * [使用情况调查增加功能和改进](https://www.surveymonkey.com/s/JS5826F) 
  * [做一点捐献](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)


######Use [AbotX](http://abotx.org) for powerful extensions/wrappers

  * [Crawl multiple sites concurrently](http://abotx.org/Learn/ParallelCrawlerEngine)
  * [Execute/Render Javascript](http://abotx.org/Learn/JavascriptRendering)
  * [Avoid getting blocked by sites](http://abotx.org/Learn/CrawlerX#crawlerx-pause-resume)
  * [Pause/Resume live crawls](http://abotx.org/Learn/CrawlerX#crawlerx-pause-resume)
  * [Schedule day/time crawl limits](http://abotx.org/Learn/Scheduler)
  * [Automatically speed up/down based on current resource usage](http://abotx.org/Learn/ThroughputMaximizer)

######使用 [AbotX](http://abotx.org) 开发强大的扩展包

  * [同时爬取多个站点](http://abotx.org/Learn/ParallelCrawlerEngine)
  * [执行渲染 Javascript](http://abotx.org/Learn/JavascriptRendering)
  * [避免被页面阻塞](http://abotx.org/Learn/CrawlerX#crawlerx-pause-resume)
  * [暂停/恢复爬虫](http://abotx.org/Learn/CrawlerX#crawlerx-pause-resume)
  * [编写定时任务](http://abotx.org/Learn/Scheduler)
  * [在当前可用的资源下自动控制速度](http://abotx.org/Learn/ThroughputMaximizer)

<br /><br />
<hr />
##Quick Start 

##快速开始

######Installing Abot 安装Abot
  * Install Abot using [Nuget](https://www.nuget.org/packages/Abot/)
  * If you prefer to build from source yourself see the [Working With The Source Code section](#working-with-the-source-code) below

  * 用 [Nuget](https://www.nuget.org/packages/Abot/) 安装
  * 如果你更喜欢自己编译可以看 [Working With The Source Code section](#working-with-the-source-code)

######Using Abot 使用Abot
1: Add the following using statements to the host class... 

1: 添加命名空间到类文件
```c#
using Abot.Crawler;
using Abot.Poco;
```
2: Configure Abot using any of the options below. You can see what effect each config value has on the crawl by looking at the [code comments ](https://github.com/sjdirect/abot/blob/master/Abot/Poco/CrawlConfiguration.cs).

2: 关于Abot的配置可以参考下面。  你可以看 [code comments ](https://github.com/sjdirect/abot/blob/master/Abot/Poco/CrawlConfiguration.cs)了解每个配置对Abot有什么影响。

**Option 1:** Add the following to the app.config or web.config file of the assembly using the library. Nuget will NOT add this for you. *NOTE: The gcServer or gcConcurrent entry may help memory usage in your specific use of abot.*

**选项 1:** Nuget不会为你添加配置文件，需要你自己添加app.config 或者 web.config到你的程序集中。 *NOTE:  gcServer 或者 gcConcurrent 节点可以帮助Abot更高效的使用内存.*
```xml
<configuration>
  <configSections>
    <section name="abot" type="Abot.Core.AbotConfigurationSectionHandler, Abot"/>
  </configSections>
  
  <runtime>
    <!-- Experiment with these to see if it helps your memory usage, USE ONLY ONE OF THE FOLLOWING -->
    <!-- 下面的2个配置可以控制内存使用量，不过请仅使用一个 -->
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

**选项 2:** 创建一个 Abot.Poco.CrawlConfiguration类实例的方式进行配置，这种方可会忽略app.config文件。
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
**选项 3:** 配置文件 类实例的方式都用
```c#
CrawlConfiguration crawlConfig = AbotConfigurationSectionHandler.LoadFromXml().Convert();
crawlConfig.MaxConcurrentThreads = 5;//this overrides the config value
etc...
```

3: Create an instance of Abot.Crawler.PoliteWebCrawler
3: 创建一个 Abot.Crawler.PoliteWebCrawler 的实例
```c#
//Will use app.config for configuration.
//用 app.config 的配置
PoliteWebCrawler crawler = new PoliteWebCrawler();
```
```c#
//Will use the manually created crawlConfig object created above
//使用上面创建的配置对象创建抓取对象
PoliteWebCrawler crawler = new PoliteWebCrawler(crawlConfig, null, null, null, null, null, null, null, null);
```
4: Register for events and create processing methods (both synchronous and asynchronous versions available)

4: 注册事件并创建处理方法（包括同步和异步2个版本）
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
5: (Optional) Add any number of custom objects to the dynamic crawl bag or page bag. These objects will be available in the CrawlContext.CrawlBag object, PageToCrawl.PageBag object or PageToCrawl.PageBag object.

5: (可选) 增加任意数量的自定义对象动态的添加到crawl bag或者page bag。这个对象在
CrawlContext.CrawlBag、PageToCrawl.PageBag中可用。
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
    //从CrawlContext对象中得到Foo实例
	CrawlContext context = e.CrawlContext;
    context.CrawlBag.MyFoo1.Bar();
    context.CrawlBag.MyFoo2.Bar();

    //Also add a dynamic value to the PageToCrawl or CrawledPage
    //当然也可以增加一个动态的值到PageToCrawl 或者 CrawledPage中
    e.PageToCrawl.PageBag.Bar = new Bar();
}
```
6: Run the crawl
6: 运行爬虫
```c#
CrawlResult result = crawler.Crawl(new Uri("http://localhost:1111/")); //This is synchronous, it will not go to the next line until the crawl has completed

if (result.ErrorOccurred)
	Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
else
	Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
```
OR run the crawl with a cancellation token to stop the crawl early
或者添加一个停止爬虫的标记在crawl中
```c#
CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

PoliteWebCrawler crawler = new PoliteWebCrawler();
CrawlResult result = crawler.Crawl(new Uri("addurihere"), cancellationTokenSource);
```

<br /><br /><br />
<hr />
##Logging (Optional)日志(可选)
Abot uses Log4Net to log messages. These log statements are a great way to see whats going on during a crawl. However, if you dont want to use log4net you can skip this section. 

Abot用Log4Net做消息记录。这些日志是看crawl在干什么的好方法，然后，如果你对crawl正在做什么不感兴趣，你可以跳过这一节。
Below is an example log4net configuration. Read more abot log4net at [their website](http://logging.apache.org/log4net/release/manual/introduction.html)

下面是一个关于log4net配置的例子。关于更多的log4net信息可以阅读 [their website](http://logging.apache.org/log4net/release/manual/introduction.html)
Add using statement for log4net. 增加log4net声明
```c#
using log4net.Config;
```

Be sure to call the following method to tell log4net to read in the config file. This call must happen before Abot's Crawl(Uri) method, otherwise you wont see any output. This is usually called in the beginning of a console app or service or the global.asax of a web app.

请确保调用下面这个方法让log4net去读取配置文件。这个方法的调用必须在 Abot's Crawl(Uri)方法调用之前，这样才会看到日志输出。这个方法通常在控制台、服务的开始或者web的global.asax中被调用。
```c#
XmlConfigurator.Configure();
```

The following configuration data should be added to the app.config file of the application that will be running Abot.
下面的配置数据应该增加到Abot的运行配置文件app.config中。
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
##Customizing Crawl Behavior 自定义抓取行为

Abot was designed to be as pluggable as possible. This allows you to easily alter the way it works to suite your needs.

Ablot 设计的是一种插件式的工作方式。它允许你根据自己的需要定义工作方式。

The easiest way to change Abot's behavior for common features is to change the config values that control them. See the [Quick Start](#quick-start) page for examples on the different ways Abot can be configured.

控制Ablot最简单的方式是配置它的配置文件。可以看 [Quick Start](#quick-start) 中的例子学习配置Ablot的不同方式。

####CrawlDecision Callbacks/Delegates  爬虫的工作方式  回调/委托
Sometimes you don't want to create a class and go through the ceremony of extending a base class or implementing the interface directly. For all you lazy developers out there Abot provides a shorthand method to easily add your custom crawl decision logic. NOTE: The ICrawlDecisionMaker's corresponding method is called first and if it does not "allow" a decision, these callbacks will not be called.

有时候我们并不想继承一个类然后再创建一个类或者实现一个接口。因此Adbot为追求简洁的开发人员提供了一种更加简便的方法在爬虫中增加你自己的逻辑。注意: 如果第一次调用 ICrawlDecisionMaker 一类的方法失败，他的回调方法不会被调用。

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

####Custom Implementations 自定义实现
PoliteWebCrawler is the master of orchestrating the crawl. Its job is to coordinate all the utility classes to "crawl" a site. PoliteWebCrawler accepts an alternate implementation for all its dependencies through its constructor.
 
PoliteWebCrawler 是爬虫框架啊的初始化类。它的工作是设置爬虫的初始地址。PoliteWebCrawler通过构造函数来接收所有的配置。
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

如果传递null将使用默认值。下面的例子中使用的自定义实现是IPageRequester和IHyperLinkParser，其他为null的值将使用默认值。

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

以下将说明 PoliteWebCrawler 依赖的每个接口所做的真正的工作。

######ICrawlDecisionMaker
The callback/delegate shortcuts are great to add a small amount of logic but if you are doing anything more heavy you will want to pass in your custom implementation of ICrawlDecisionMaker. The crawler calls this implementation to see whether a page should be crawled, whether the page's content should be downloaded and whether a crawled page's links should be crawled.

用回调/委托的方式增加逻辑是简便的，但是如果你想用ICrawlDecisionMaker做自定义的操作将会变得非常繁琐。这个方法的调用决定了什么页面应该被爬取，什么页面应该被下载，什么页面不应该被爬取。

[CrawlDecisionMaker.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/CrawlDecisionMaker.cs) is the default ICrawlDecisionMaker used by Abot. This class takes care of common checks like making sure the config value MaxPagesToCrawl is not exceeded. Most users will only need to create a class that extends CrawlDecision maker and just add their custom logic. However, you are completely free to create a class that implements ICrawlDecisionMaker and pass it into PoliteWebCrawlers constructor.

Abot默认使用的ICrawlDecisionMaker是 [CrawlDecisionMaker.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/CrawlDecisionMaker.cs) 。这个类确保配置文件中的MaxPagesToCrawl值可以起到正确的作用。大多数用户只是创建一个继承自CrawlDecision的类来增加自己的逻辑。然而你完全可以创建一个ICrawlDecisionMaker类把它传递到PoliteWebCrawlers的构造函数中。

```c#
/// <summary>
/// Determines what pages should be crawled, whether the raw content should be downloaded and if the links on a page should be crawled
///决定已经爬取的页面上那个连接应该被下载，那个连接应该被继续爬取
/// </summary>
public interface ICrawlDecisionMaker
{
	/// <summary>
	/// Decides whether the page should be crawled 决定页面应该被继续爬取
	/// </summary>
	CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext);

	/// <summary>
	/// Decides whether the page's links should be crawled 决定页面不应该被爬取
	/// </summary>
	CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext);

	/// <summary>
	/// Decides whether the page's content should be dowloaded 决定页面被下载
	/// </summary>
	CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext);
}
```


######IThreadManager
The IThreadManager interface deals with the multithreading details. It is used by the crawler to manage concurrent http requests. 

IThreadManager接口配置了多进程的运行信息。可以使用它在爬虫中进行http请求的并发。

[TaskThreadManager.cs](https://github.com/sjdirect/abot/blob/master/Abot/Util/TaskThreadManager.cs) is the default IThreadManager used by Abot. 

[TaskThreadManager.cs](https://github.com/sjdirect/abot/blob/master/Abot/Util/TaskThreadManager.cs)  是Abot默认使用的线程管理器

```c#
/// <summary>
/// Handles the multithreading implementation details 多线程的实现细节
/// </summary>
public interface IThreadManager : IDisposable
{
	/// <summary>
	/// Max number of threads to use. 最多多少个线程可以使用
	/// </summary>
	int MaxThreads { get; }

	/// <summary>
	/// Will perform the action asynchrously on a seperate thread
  /// 在一个单独的异步线程中执行的动作
	/// </summary>
	/// <param name="action">The action to perform</param>
	void DoWork(Action action);

	/// <summary>
	/// Whether there are running threads
  /// 是否有正在运行的线程
	/// </summary>
	bool HasRunningThreads();

	/// <summary>
	/// Abort all running threads
  /// 推出所有线程
	/// </summary>
	void AbortAll();
}
```


######IScheduler 调度
The IScheduler interface deals with managing what pages need to be crawled. The crawler gives the links it finds to and gets the pages to crawl from the IScheduler implementation. A common use cases for writing your own implementation might be to distribute crawls across multiple machines which could be managed by a DistributedScheduler.

IScheduler 接口详细管理了什么页面需要被抓取。从IScheduler的实现中去取出一个连接让爬虫进行抓取。在分布式调度中一个常见的编程用例是你编写你自己的爬虫实现分发到不同的机器上。

[Scheduler.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/Scheduler.cs) is the default IScheduler used by the crawler and by default is constructed with in memory collection to determine what pages have been crawled and which need to be crawled. 

[Scheduler.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/Scheduler.cs) 是爬虫中IScheduler的默认实现，它管理了内存集合中的连接 哪些连接已经被抓取哪些连接没有被抓取。

```c#
/// <summary>
/// Handles managing the priority of what pages need to be crawled
/// 决定了什么页面应该被优先爬取
/// </summary>
public interface IScheduler
{
	/// <summary>
	/// Count of remaining items that are currently scheduled
  /// 对目前已经抓取的页面进行计数
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Schedules the param to be crawled
  /// 增加一个抓取页面
	/// </summary>
	void Add(PageToCrawl page);

	/// <summary>
	/// Schedules the param to be crawled
  /// 增加一个抓取页面
	/// </summary>
	void Add(IEnumerable<PageToCrawl> pages);

	/// <summary>
	/// Gets the next page to crawl 得到下一个要抓取的页面
	/// </summary>
	PageToCrawl GetNext();

	/// <summary>
	/// Clear all currently scheduled pages 清除所以目前抓取的页面
	/// </summary>
	void Clear();
}
```


######IPageRequester
The IPageRequester interface deals with making the raw http requests.

IPageRequester 接口可以使用远程的http请求。

[PageRequester.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/PageRequester.cs) is the default IPageRequester used by the crawler. 

[PageRequester.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/PageRequester.cs) 是爬虫使用的IPageRequester默认实现。

```c#
public interface IPageRequester
{
	/// <summary>
	/// Make an http web request to the url and download its content
  /// 创造一个针对url的HTTP Web请求并下载内容
	/// </summary>
	CrawledPage MakeRequest(Uri uri);

	/// <summary>
	/// Make an http web request to the url and download its content based on the param func decision
  /// 发起一个到url的http web 请求  根据传递的参数func决定下载的内容
	/// </summary>
	CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
}
```

######IHyperLinkParser
The IHyperLinkParser interface deals with parsing the links out of raw html.

IHyperLinkParser 接口从原生的html页面中解析出链接。

[HapHyperlinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/HapHyperLinkParser.cs) is the default IHyperLinkParser used by the crawler. It uses the well known parsing library [Html Agility Pack](http://htmlagilitypack.codeplex.com/). There is also an alternative implementation [CsQueryHyperLinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/CsQueryHyperLinkParser.cs) which uses [CsQuery](https://github.com/jamietre/CsQuery) to do the parsing. CsQuery uses a css style selector like jquery but all in c#. 

[HapHyperlinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/HapHyperLinkParser.cs) 是爬虫默认的IHyperLinkParser实现。它用了一个著名的html解析库 [Html Agility Pack](http://htmlagilitypack.codeplex.com/).它也可以用  [CsQueryHyperLinkParser.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/CsQueryHyperLinkParser.cs) 替换，CsQueryHyperLinkParser.cs用了 [CsQuery](https://github.com/jamietre/CsQuery) 解析。CsQuery 可以让你在c#中像jquery一样使用css选择符。

```c#
/// <summary>
/// Handles parsing hyperlikns out of the raw html
/// 分析处理从html中解析出来的链接
/// </summary>
public interface IHyperLinkParser
{
	/// <summary>
	/// Parses html to extract hyperlinks, converts each into an absolute url
  /// 解析html中的超链接，并转换成绝对链接
	/// </summary>
	IEnumerable<Uri> GetLinks(CrawledPage crawledPage);
}
```

######IMemoryManager
The IMemoryManager handles memory monitoring. This feature is still experimental and could be removed in a future release if found to be unreliable. 

IMemoryManager 对象可以实现内存监控。这个功能是个实验性功能，如果不稳定的话这个功能未来可能会删除。

[MemoryManager.cs](https://github.com/sjdirect/abot/blob/master/Abot/Util/MemoryManager.cs) is the default implementation used by the crawler. 

[MemoryManager.cs](https://github.com/sjdirect/abot/blob/master/Abot/Util/MemoryManager.cs) 是爬虫中IMemoryManager的默认的实现。

```c#
/// <summary>
/// Handles memory monitoring/usage
/// 监控内存的使用
/// </summary>
public interface IMemoryManager : IMemoryMonitor, IDisposable
{
	/// <summary>
	/// Whether the current process that is hosting this instance is allocated/using above the param value of memory in mb
  /// 标志此实例在主机中 申请/使用的内存是否在这个值之上
	/// </summary>
	bool IsCurrentUsageAbove(int sizeInMb);

	/// <summary>
	/// Whether there is at least the param value of available memory in mb
  /// 最少有多少mb的内存可以使用
	/// </summary>
	bool IsSpaceAvailable(int sizeInMb);
}
```

######IDomainRateLimiter
The IDomainRateLimiter handles domain rate limiting. It will handle determining how much time needs to elapse before it is ok to make another http request to the domain.

IDomainRateLimiter句柄可以限制一个域名的爬取速率。这可以决定在一个域名抓取之后经过多长时间再发起另一个域名爬取请求。

[DomainRateLimiter.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/DomainRateLimiter.cs) is the default implementation used by the crawler. 

[DomainRateLimiter.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/DomainRateLimiter.cs) 是爬虫使用的 IDomainRateLimiter 默认实现。
```c#
/// <summary>
/// Rate limits or throttles on a per domain basis
/// 限制每个域名的基础爬取速率
/// </summary>
public interface IDomainRateLimiter
{
	/// <summary>
	/// If the domain of the param has been flagged for rate limiting, it will be rate limited according to the configured minimum crawl delay
  /// 如果域名已经被标记为速率限制，那么爬取的速率值将会根据配置文件进行限定，
	/// </summary>
	void RateLimit(Uri uri);

	/// <summary>
	/// Add a domain entry so that domain may be rate limited according the the param minumum crawl delay
  /// 增加一个域名条目，根据传递参数设置的最小延迟限制爬取速率
	/// </summary>
	void AddDomain(Uri uri, long minCrawlDelayInMillisecs);
}
```


######IRobotsDotTextFinder
The IRobotsDotTextFinder is responsible for retrieving the robots.txt file for every domain (if isRespectRobotsDotTextEnabled="true") and building the robots.txt abstraction which implements the IRobotsDotText接口的实现。 interface. 

IRobotsDotTextFinder 负责为每个域名检索robots.txt文件(如果isRespectRobotsDotTextEnabled="true")并绑定robots.txt文件到IRobotsDotText接口的实现。

[RobotsDotTextFinder.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/RobotsDotTextFinder.cs) is the default implementation used by the crawler. 

[RobotsDotTextFinder.cs](https://github.com/sjdirect/abot/blob/master/Abot/Core/RobotsDotTextFinder.cs) 是爬虫IRobotsDotTextFinder的默认实现。

```c#
/// <summary>
/// Finds and builds the robots.txt file abstraction
/// 查找并绑定 robots.txt的抽象实现
/// </summary>
public interface IRobotsDotTextFinder
{
	/// <summary>
	/// Finds the robots.txt file using the rootUri. 
  /// 在robots.txt文件中提取rootUri
	IRobotsDotText Find(Uri rootUri);
}
```

<br /><br /><br />
<hr />
##Working With The Source Code 使用源代码
The most common way to customize crawl behavior is by extending classes and overriding methods. You can also create a custom implementation of a core interface. All this can be done outside of Abot's source code. 

自定义爬虫行为最常见的方式是继承类和重写方法。也可以在接口代码中创建自定义实现。所有这一切可以定义出超出Abot's源代码功能的实现。

However, if the changes that you are going to make are out of the ordinary or you want to contribute a bug fix or feature then you will want to work directly with Abot's source code. Below you will find what you need to get the solution building/running on your local machine.

然而，如果你做的更改有一些特殊功能或者想添加新功能、修复bug 也可以直接在 Abot's的源代码上工作。下面你需要将解决方案 编译/运行 在本地计算机上。

######Your First Build 自己编译
1: Clone the latest using the following commands<br /><br />
  *git clone git@github.com:sjdirect/abot.git*<br /><br />
2: Open the Abot.sln file in Visual Studio (all dev done in vs 2013 premium)<br />
3: Build the solution normally<br />

1: 用下面的命令<br /><br />
  *git clone git@github.com:sjdirect/abot.git*<br /><br />克隆最新的版本。
2: 打开Abot.sln 文件用Visual Studio (all dev done in vs 2013 premium)<br />
3: 生成解决方案<br />

######External Tools Needed 需要的外部工具
NUnit Test Runner: The unit tests for Abot are using NUnit which is not supported right out of the box in visual studio. You must either install a NUnit test adapter or a product like TestDriven、 or Resharper. Download the [NUnit test adapter](http://visualstudiogallery.msdn.microsoft.com/6ab922d0-21c0-4f06-ab5f-4ecd1fe7175d) or install it through visual studio extension manager.

运行单元测试: Abot的单元测试使用的是 NUnit ，不过在visual studio中不支持黑盒/白盒测试。你需要安装NUnit  test adapter、TestDriven、Resharper的其中之一。可以通过 [NUnit test adapter](http://visualstudiogallery.msdn.microsoft.com/6ab922d0-21c0-4f06-ab5f-4ecd1fe7175d) 下载或者使用visual studio的扩展管理工具安装。

######Solution Project/Assembly Overview 解决方案 项目/程序集 概览
* **Abot:** Main library for all crawling and utility code.<br />
* **Abot:** 主要的代码库包括爬虫和单元测试代码
* **Abot.Demo:** Simple console app that demonstrates how to use abot.<br />
* **Abot.Demo:** 简单的控制台实现演示怎样使用abot<br />
* **Abot.SiteSimulator:** An asp.net mvc application that can simulate any number of pages and several http responses that are encountered during a crawl. This site is used to produce a predictable site crawl for abot.
Both Abot.Tests.Unit and Abot.Tests.Integration make calls to this site. However a sample of those calls were saved/stored in a fiddler session and are automatically replayed by FiddlerCore everytime the unit or integration tests are run. <br />
* **Abot.SiteSimulator:** Asp.net mvc 应用程序，在爬虫运行过程中可以模拟任意数量的页面和几种 http 响应。这个网站可以为abot爬虫产生具有代表性的网页。 Abot.Tests.Unit和Abot.Tests.Integration都可以使用这个网站。但是每个请求的样本都会 保存/存储 作为一个fiddler session，每次通过FiddlerCore运行unit或者integration的时候都会再次使用。
* **Abot.Tests.Unit:** Unit tests for all Abot assemblies. Abot.SiteSimulator site must be running for tests to pass since mocking http web requests is more trouble then its worth.<br />
* **Abot.Tests.Unit:** 单元测试是Abot的组件之一. Abot.SiteSimulator 网站必须通过测试的方式运行，因为通过mocking发起一个http web请求的过程可以发现问题，这样才能体现它的价值.<br />
* **Abot.Tests.Integration:** Tests the end to end crawl behavior. These are real crawls, no mocks/stubs/etc.. Abot.SiteSimulator site must be running for tests to pass.<br />
* **Abot.Tests.Integration:**测试端到端的行为.这是真正的爬虫, 不是 mocks/stubs/etc.. Abot.SiteSimulator 站点需要通过测试的方式运行.<br />

######How to run Abot.Demo 怎样运行 Abot.Demo
The demo project has a few config values set that greatly limit Abot's speed.  This is to make sure you don't get banned by your isp provider or get blocked by the sites you are crawling. These setting are..

这个演示项目有几个配置值，极大地限制 Abot's的速度。这是为了确保你不会被你的ISP提供商拒绝服务或者网站屏蔽你的爬虫。这些设置都是必要的..

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

这个配置的Abot将使用1个线程，只爬取10个页面并且在每个http请求之间必须间隔1秒。如果你想感受Abot的实际速度，可以切换到下面的配置。

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

这个Abot配置用10个线程，爬取1000个页面在，2个requests之间不等待。Abot在同一时间将会处理超过10个页面。

1: Right click on the Abot.Demo project and set it as the "startup project"<br />
2: Then hit ctrl + F5 to see the console app run.<br />
3: When prompted for a url enter whatever site you want to crawl (must begin with "http://" or "https://")<br />
4: Press enter<br />
5: View the Abot.Demo/bin/debug/abotlog.txt file for all the output.<br />

1: 右键单击  Abot.Demo项目，将次设置为 "startup project"<br />
2: 然后 ctrl + F5 启动控制台应用.<br />
3: 当提示输入url回车的时候输入你想爬取的页面 (必须以 "http://" or "https://" 开始)<br />
4: 回车<br />
5:在 Abot.Demo/bin/debug/abotlog.txt 文件中查看输出.<br />

######How To Run Demo Against Abot.SiteSimulator 如何运行 Against Abot.SiteSimulator

If you would rather test your crawls on a test site then I would suggest you use the Abot.SiteSimulator mvc project. This site is hosted on your machine and will not generate any http traffic beyond your local network. This allows you to crawl as aggressively as you would like without of fear of isp issues. This site also has links that purposefully return http 200, 301-302, 403-404 and 500 responses to simulate a wide range of what can be encountered while crawling the web. To use the Abot.SiteSimulator project do the following...

如果你希望测试你的爬虫在一个测试站点，我建议你用我的Abot.SiteSimulator mvc项目。这个站点运行在你的机器上不会在本地以外的网络产生任何流量。这样允许你配置更积极的爬虫策略，摆脱ISP的问题。这个站点也有链接可以返回200, 301-302, 403-404 and 500等状态码，来模拟在实际的网络运行环境中可能遇到的各种问题。使用Abot.SiteSimulator项目可以按照下面的步奏进行操作...

1: Right click on the Abot.SiteSimulator project and set it as the "startup project".<br /> 
2: Then hit ctrl + F5 to run it, You should see a simple webpage with a few links on http://localhost:1111/<br />
3: Right click on the Abot.Demo project and set it as the "startup project". <br />
4: Then hit ctrl + F5 to see the console app run.<br />
5: When prompted for a url enter: http://localhost:1111/<br />
6: Press enter<br />
7: View the Abot.Demo/bin/debug/abotlog.txt file for all the output.<br />

1: 右键 Abot.SiteSimulator 项目把它作为 "startup project".<br /> 
2: 按下 ctrl + F5 运行它, 可以在这个连接里面 http://localhost:1111/ 查看页面<br />
3: 右键 Abot.Demo 项目作为 "startup project". <br />
4: 按下 ctrl + F5 运行控制台程序.<br />
5: 出入这个url回车: http://localhost:1111/<br />
6: 回车<br />
7:在 Abot.Demo/bin/debug/abotlog.txt 这个文件中查看输出.<br />

Now the Abot.Demo console application will be crawling the Abot.SiteSimulator test site that is running locally on your machine. This is the best way to develop Abot without being banned by your ip.

Abot.Demo 控制台应用会爬取在你本机上运行的 Abot.SiteSimulator test 网站。如果开发一个爬虫应用程序这是不受ip限制的最好办法。

######How to run Abot.Tests.Unit 怎样运行 Abot.Tests.Unit
1: Verify "External Tools" defined above are installed and working<br />
2: Run Abot.Tests.Unit tests using whatever test runner you like (Visual Studio test runner, Testdriven.net or Resharper).<br />

1: 验证扩展工具都已经安装并且工作<br />
2: 运行 Abot.Tests.Unit 测试你喜欢的功能 (Visual Studio test runner, Testdriven.net or Resharper).<br />

######How to run Abot.Tests.Integration 怎么运行 Abot.Tests.Integration
1: Verify "External Tools" defined above are installed and working<br />
2: Run Abot.Tests.Integration using whatever test runner you like (Visual Studio test runner, Testdriven.net or Resharper).<br />
3: View the file output at Abot.Tests.Integration/bin/debug/abotlog.txt file for all the output.<br />

1: 验证扩展工具都已经安装并且工作<br />
2: 运行 Abot.Tests.Integration 测试你喜欢的功能 (Visual Studio test runner, Testdriven.net or Resharper).<br />
3: 在 Abot.Tests.Integration/bin/debug/abotlog.txt 中查看输出.<br />

######Fiddler Core Fiddler核心
Just a note that Fiddler.Core is started and stopped during unit and integration tests. This allows replaying predictable http requests. Read more about [Fiddler Core here](http://www.telerik.com/fiddler/fiddlercore)

仅仅需要注意的是Fiddler.Core的开始和停止是在单元测试和集成测试中.Fiddler支持发起循环的http请求 . 关于更多的请阅读 [Fiddler Core here](http://www.telerik.com/fiddler/fiddlercore)
