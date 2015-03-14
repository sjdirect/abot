abot [![Build Status](https://ci.appveyor.com/api/projects/status/b1ukruawvu6uujn0?svg=true)](https://ci.appveyor.com/project/sjdirect/abot)
====

C# web crawler build for speed and flexibility.

Abot is an open source C# web crawler built for speed and flexibility. It takes care of the low level plumbing (multithreading, http requests, scheduling, link parsing, etc..). You just register for events to process the page data. You can also plugin your own implementations of core interfaces to take complete control over the crawl process.

Abot targets .NET version 4.0.

  * [Ask questions and search for answers on the Community Forum](http://groups.google.com/group/abot-web-crawler)<br />
  * [Report Bugs or Suggest Features](https://github.com/sjdirect/abot/issues)
  * [Consider making a donation](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=G6ZY6BZNBFVQJ)

###What's So Great About It?
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
  1. Add the following using statements.. 
```c#
using Abot.Crawler;
using Abot.Poco;
```
 2. Configure Abot using *ONE* of the following methods. Look at the [code comments to](https://github.com/sjdirect/abot/blob/master/Abot/Poco/CrawlConfiguration.cs) comments to see what effect each config value has on the crawl.
    1. Add the following to the app.config or web.config file of the assembly using the library. Nuget will NOT add this for you. *NOTE: The gcServer or gcConcurrent entry may help memory usage in your specific use of abot.*
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
