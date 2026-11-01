# Upgrading from Abot 2.x to 3.0

Abot 3.0 modernises the library: it targets .NET 10, .NET 8 and .NET Framework alongside .NET Standard 2.0, updates its dependencies to current, patched versions, and makes disposal and concurrency behave the way .NET developers expect. The crawler now runs on async workers instead of blocking threads, takes a `CancellationToken`, and no longer polls. Most applications that only use `PoliteWebCrawler` with its default components upgrade without code changes, apart from passing a `CancellationToken` instead of a `CancellationTokenSource`. Applications that pass their own components into the crawler, implement Abot's interfaces, or derive from Abot's classes need to read the sections below.

3.0 is currently published as a pre-release:

```shell
dotnet add package Abot --version 3.0.0-preview.1
```

## At a glance

| Change | Who is affected | What to do |
|---|---|---|
| [Abot no longer disposes objects you pass in](#1-abot-no-longer-disposes-objects-you-pass-in) | Anyone passing their own components or `HttpClient` to a constructor | Dispose what you create, after the crawler |
| [`Dispose()` is no longer virtual](#2-dispose-is-no-longer-virtual-override-disposebool) | Classes that derive from Abot types and override `Dispose()` | Override `Dispose(bool)` instead |
| [`ObjectDisposedException` after `Dispose`](#3-using-an-object-after-dispose-throws-objectdisposedexception) | Code that uses an Abot object after disposing it | Don't use objects after disposing them |
| [Dependency upgrades](#4-supported-platforms-and-dependencies) | Apps using AngleSharp or Serilog APIs directly, or older Serilog sinks | Check the AngleSharp and Serilog versions you use |
| [`HttpServicePointConnectionLimit` now applies on .NET 8 and later](#51-httpservicepointconnectionlimit-now-works-on-net-8-and-later) | Apps on .NET 8+ that relied on unlimited connections per host | Raise the limit or set it to `0` |
| [Other behaviour changes](#5-behaviour-changes) | Mostly subclasses of `WebCrawler` | Review the list |
| [Async workers; thread managers are obsolete](#61-pages-are-processed-by-async-workers-thread-managers-are-obsolete) | Anyone passing a thread manager, or relying on more than `MaxConcurrentThreads` requests at once | Use the constructors without a thread manager; tune `MaxConcurrentThreads` |
| [`CrawlAsync` takes a `CancellationToken`](#62-crawlasync-takes-a-cancellationtoken) | Anyone passing a `CancellationTokenSource` to `CrawlAsync` | Pass `source.Token` instead |
| [Interfaces take a `CancellationToken`; `IScheduler` changed](#63-interface-changes) | Anyone implementing Abot's interfaces | Add the parameters and the new `IScheduler` members |
| [New protected members on `WebCrawler`](#64-classes-that-derive-from-webcrawler) | Subclasses overriding `ProcessPage`, `CrawlThePage`, `CrawlSite`, `WaitMinimumRetryDelay` or `CrawlAsync` | Override the `CancellationToken` overloads |
| [`DomainRateLimiter` internals replaced](#65-classes-that-derive-from-domainratelimiter) | Subclasses of `DomainRateLimiter` | Override `GetDomainSchedule` instead of `GetRateLimiter` |

## 1. Abot no longer disposes objects you pass in

In 2.x, disposing a crawler disposed everything it held, including components you passed into its constructor. That made it impossible to share an object between crawlers, for example one `HttpClient` for the whole application, and it disposed objects the crawler did not own.

In 3.0 the rule is the standard .NET one: **whoever creates an object disposes it.**

- Objects Abot creates itself, because you passed `null` or used a constructor without that parameter, are disposed by Abot.
- Objects you pass in are yours. Abot never disposes them, so you must dispose them, after you dispose the crawler.

These constructor parameters were disposed by Abot in 2.x and must now be disposed by you:

| Constructor | Parameters you now dispose yourself |
|---|---|
| `WebCrawler` / `PoliteWebCrawler` | `scheduler`, `pageRequester`, `memoryManager`, and `threadManager` on the [obsolete constructors](#61-pages-are-processed-by-async-workers-thread-managers-are-obsolete) |
| `PageRequester` | `httpClient` |
| `Scheduler` | `crawledUrlRepo`, `pagesToCrawlRepo` |
| `MemoryManager` | `memoryMonitor` |

Abot never disposed the other objects you can pass in, in 2.x either: `PageRequester`'s `contentExtractor`, `TaskThreadManager`'s `cancellationTokenSource`, `CachedMemoryMonitor`'s `memoryMonitor`, and `PoliteWebCrawler`'s `domainRateLimiter` and `robotsDotTextFinder`. Dispose those too if they are disposable.

### Example: default components (no change needed)

If you don't pass any components in, Abot creates and disposes everything itself:

```csharp
using var crawler = new PoliteWebCrawler(new CrawlConfiguration { MaxPagesToCrawl = 10 });
crawler.PageCrawlCompleted += (sender, e) => Console.WriteLine(e.CrawledPage.Uri);

var result = await crawler.CrawlAsync(new Uri("https://example.com/"));
```

### Example: custom components

**2.x**: disposing the crawler also disposed the scheduler and page requester:

```csharp
var config = new CrawlConfiguration();
var crawler = new PoliteWebCrawler(config, null, null,
    new Scheduler(), new PageRequester(config, new WebContentExtractor()), null, null, null, null);

await crawler.CrawlAsync(new Uri("https://example.com/"));
crawler.Dispose();   // also disposed the Scheduler and PageRequester
```

**3.0**: you created them, so you dispose them. With `using var` they are disposed in reverse order of declaration, so the crawler goes first:

```csharp
var config = new CrawlConfiguration();

using var contentExtractor = new WebContentExtractor();
using var pageRequester = new PageRequester(config, contentExtractor);
using var scheduler = new Scheduler();
using var crawler = new PoliteWebCrawler(config, null, scheduler, pageRequester, null, null, null, null);

await crawler.CrawlAsync(new Uri("https://example.com/"));
// crawler, scheduler, pageRequester and contentExtractor are disposed here, in that order
```

The 3.0 constructor no longer has a thread manager parameter (see [section 6.1](#61-pages-are-processed-by-async-workers-thread-managers-are-obsolete)), so it takes one argument fewer.

### Example: sharing one `HttpClient` across crawls

Because Abot no longer disposes an `HttpClient` you pass in, you can now reuse one for many crawls, which is how `HttpClient` is meant to be used:

```csharp
// Created once, for example as a singleton, and disposed when the application shuts down
var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyCrawler/1.0");

async Task<CrawlResult> CrawlAsync(Uri site)
{
    var config = new CrawlConfiguration { MaxPagesToCrawl = 100 };

    using var contentExtractor = new WebContentExtractor();
    using var pageRequester = new PageRequester(config, contentExtractor, httpClient);
    using var crawler = new PoliteWebCrawler(config, null, null, pageRequester, null, null, null, null);

    return await crawler.CrawlAsync(site);   // httpClient is still usable afterwards
}
```

When you pass in an `HttpClient`, `PageRequester` uses it as it is. The HTTP settings in `CrawlConfiguration` (user agent, timeout, redirects, cookies, decompression, certificate validation, login and connection limit) are only applied to the client Abot builds itself, so configure your own client accordingly.

### Example: memory monitoring

`MemoryManager` no longer disposes the monitor you give it. A `CachedMemoryMonitor` runs a timer, so it must be disposed:

```csharp
var config = new CrawlConfiguration { MaxMemoryUsageInMb = 1024 };

using var gcMonitor = new GcMemoryMonitor();
using var cachedMonitor = new CachedMemoryMonitor(gcMonitor, cacheExpirationInSeconds: 5);
using var memoryManager = new MemoryManager(cachedMonitor);
using var crawler = new PoliteWebCrawler(config, null, null, null, null, memoryManager, null, null);
```

If you don't need a custom monitor, leave `memoryManager` as `null` and Abot creates and disposes one for you whenever a memory limit is configured.

## 2. `Dispose()` is no longer virtual: override `Dispose(bool)`

Every disposable class in Abot now follows the standard .NET [dispose pattern](https://learn.microsoft.com/dotnet/standard/garbage-collection/implementing-dispose): a public, non-virtual `Dispose()` and a `protected virtual void Dispose(bool disposing)` for derived classes. `Dispose()` can be called more than once, and from several threads at the same time, without failing.

This affects `WebCrawler`, `PoliteWebCrawler`, `ThreadManager`, `TaskThreadManager`, `ManualThreadManager`, `PageRequester`, `Scheduler`, `WebContentExtractor`, `MemoryManager`, `GcMemoryMonitor`, `CachedMemoryMonitor`, `RateLimiter`, `DomainRateLimiter` and the `InMemory`, `Compact` and `BloomFilter` crawled-URL repositories and `FifoPagesToCrawlRepository`.

If your class overrides `Dispose()`, it no longer compiles (`CS0506: cannot override inherited member because it is not marked virtual`).

**2.x**:

```csharp
public class MyCrawler : PoliteWebCrawler
{
    private readonly MyResultStore _store = new MyResultStore();

    public MyCrawler(CrawlConfiguration config) : base(config) { }

    public override void Dispose()
    {
        _store.Dispose();
        base.Dispose();
    }
}
```

**3.0**: override `Dispose(bool)`, release your own resources only when `disposing` is `true`, guard against being called twice, and always call the base class:

```csharp
public class MyCrawler : PoliteWebCrawler
{
    private readonly MyResultStore _store = new MyResultStore();
    private bool _disposed;

    public MyCrawler(CrawlConfiguration config) : base(config) { }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        // The base class stops the crawl threads first, so call it before
        // disposing anything those threads might still be using
        base.Dispose(disposing);

        if (disposing)
            _store.Dispose();
    }
}
```

The same applies to custom thread managers, if you still have one ([thread managers are obsolete](#61-pages-are-processed-by-async-workers-thread-managers-are-obsolete)):

```csharp
public class MyThreadManager : ThreadManager
{
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1);
    private bool _disposed;

    public MyThreadManager(int maxThreads) : base(maxThreads) { }

    protected override void RunActionOnDedicatedThread(Action action)
    {
        new Thread(() => RunAction(action)).Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        base.Dispose(disposing);   // aborts running work and releases the base class's resources

        if (disposing)
            _gate.Dispose();
    }
}
```

Classes that implement Abot's **interfaces** directly (`IPageRequester`, `IScheduler`, `IThreadManager` and so on) are not affected: the interfaces still just extend `IDisposable`.

### `ThreadManager._isDisplosed` is now `_isDisposed`

The misspelled protected field in `ThreadManager` is renamed to `_isDisposed`, and it is now `volatile`. Rename any references in derived classes.

## 3. Using an object after `Dispose` throws `ObjectDisposedException`

In 2.x, using an Abot object after disposing it usually failed with a `NullReferenceException`, or appeared to work while using released resources. In 3.0 it throws `ObjectDisposedException`, as .NET types do:

| Type | Members that throw after `Dispose` |
|---|---|
| `WebCrawler`, `PoliteWebCrawler` | `CrawlAsync` |
| `PageRequester` | `MakeRequestAsync` |
| `Scheduler` | `Count`, `Add`, `GetNext`, `Clear`, `AddKnownUri`, `IsUriKnown` |
| `ThreadManager` and derived classes | `DoWork` (was `InvalidOperationException`; after `AbortAll()` it is still `InvalidOperationException`) |
| `DomainRateLimiter` | `RateLimitAsync`, `RateLimit`, `AddDomain`, `AddOrUpdateDomain` |
| `RateLimiter` | `WaitToProceed` (unchanged, but the message is now the type name) |
| `FifoPagesToCrawlRepository` | `Add`, `GetNext`, `Clear`, `Count` |
| `InMemoryCrawledUrlRepository`, `CompactCrawledUrlRepository` | `Contains`, `AddIfNew` |

If you catch `InvalidOperationException` from `ThreadManager.DoWork` to detect a disposed thread manager, catch `ObjectDisposedException` instead.

## 4. Supported platforms and dependencies

### Target frameworks

The package now contains five builds, where 2.x had one (`netstandard2.0`):

| Your application runs on | Build used |
|---|---|
| .NET 10 or later | `net10.0` |
| .NET 8 and 9 | `net8.0` |
| .NET Framework 4.7.2 to 4.8.1 | `net472` |
| .NET Framework 4.6.2 to 4.7.1 | `net462` |
| .NET Core 2.0 to .NET 7, and other .NET Standard 2.0 platforms such as Mono and Unity | `netstandard2.0` |

The `net8.0` and `net10.0` builds are trim- and AOT-compatible: Abot itself causes no trim or AOT warnings when you publish with `PublishTrimmed` or `PublishAot`. Code of your own that uses `CrawlBag` or `PageBag` as `dynamic` (`crawler.CrawlBag.MyFoo = ...`) does cause them, because `dynamic` needs runtime code generation. In a trimmed or AOT application, use the bags as `IDictionary<string, object>` instead. Convert them through `object`, because a cast straight from `dynamic` is itself a dynamic operation:

```csharp
var crawlBag = (IDictionary<string, object>)(object)crawler.CrawlBag;
crawlBag["MyFoo"] = new Foo();
```

The minimum .NET Framework version is now **4.6.2**, because that is the oldest version Abot's updated dependencies support. .NET Framework applications now get a build made for .NET Framework instead of the .NET Standard one, which avoids the extra facade assemblies and binding redirects that .NET Standard libraries can need on .NET Framework 4.6.2 to 4.7.1.

Dependencies now differ per build:

- **`net10.0`** no longer pulls in `Microsoft.CSharp`, `System.Net.Http` or `System.Security.Cryptography.X509Certificates`, because they are part of .NET.
- **`net8.0`** no longer pulls in `Microsoft.CSharp` either. It has `System.Net.Http` (4.3.4) and `System.Security.Cryptography.X509Certificates` (4.3.2) as explicit dependencies, for the same reason as `netstandard2.0` below: NuGet removes the vulnerable 4.1.0 versions that NRobotsCore pulls in only for applications that target .NET 10 or later. At run time .NET 8 and 9 use their own, newer copies of these assemblies.
- **`net462`** and **`net472`** use `Microsoft.CSharp`, `System.Net.Http` and `System.Net.Http.WebRequest` from .NET Framework itself, so it doesn't pull in those packages either, including the vulnerable `System.Net.Http` 4.1.0 that NRobotsCore brought in through 2.x's .NET Standard build. It depends on `Microsoft.Bcl.TimeProvider`, which provides `TimeProvider` (see [section 5.8](#58-abot-can-run-on-a-clock-you-control)).
- **`netstandard2.0`** has `System.Net.Http` (4.3.4) and `System.Security.Cryptography.X509Certificates` (4.3.2) as explicit dependencies. They replace the vulnerable 4.1.0 versions that NRobotsCore pulled in before (GHSA-7jgj-8wvc-jh57 and GHSA-7mfr-774f-w5r9). It also depends on `Microsoft.Bcl.TimeProvider`; `TimeProvider` is built into .NET 8 and later.

### Dependency upgrades

| Package | 2.x | 3.0 |
|---|---|---|
| AngleSharp | 0.14.0 | 1.8.5 |
| Serilog | 2.10.0 | 4.4.0 |
| Serilog.Sinks.Console | 3.1.1 | 6.1.1 |
| Serilog.Enrichers.Thread | 3.1.0 | 4.0.0 |

**AngleSharp**: 0.14.0 has a known vulnerability (GHSA-pgww-w46g-26qg). If your code works with `CrawledPage.AngleSharpHtmlDocument` beyond basic querying, check it against the [AngleSharp changelog](https://github.com/AngleSharp/AngleSharp/blob/devel/CHANGELOG.md) for the 1.x releases. If your application references AngleSharp itself, it must now reference 1.8.5 or later.

**Serilog**: Abot still logs through Serilog's static `Log.Logger`, so you configure logging exactly as before:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: Constants.LogFormatTemplate)
    .CreateLogger();
```

Your application now gets Serilog 4.4.0 or later. If you use other Serilog sinks or enrichers, update them to versions that support Serilog 4.

## 5. Behaviour changes

### 5.1 `HttpServicePointConnectionLimit` now works on .NET 8 and later

`CrawlConfiguration.HttpServicePointConnectionLimit` (default 200) limits the number of concurrent connections per host. In 2.x it set `ServicePointManager.DefaultConnectionLimit`, which has no effect on `HttpClient` on .NET Core and later, so on modern .NET the limit was silently ignored.

On the `net8.0` and `net10.0` builds it now sets `HttpClientHandler.MaxConnectionsPerServer`, so a crawl on .NET 8 or later really is limited to 200 connections per host by default. That limit applies only to the crawler's own `HttpClient`. Nothing changes on the other builds: on .NET Framework (`net462` and `net472`) the limit works as before, and on .NET Core 2.0 to .NET 7 (`netstandard2.0`) it is still ignored.

On those other builds, Abot still sets `ServicePointManager.DefaultConnectionLimit` whenever a `PageRequester` is created, as 2.x did. That setting is process-wide: on .NET Framework it changes the limit for every `HttpClient` and `HttpWebRequest` in your application, and the last crawler created wins. If your application relies on its own value, set `HttpServicePointConnectionLimit` to `0`, so that Abot leaves it alone.

To keep the 2.x behaviour on .NET 8 and later (no limit set by Abot), set it to `0`:

```csharp
var config = new CrawlConfiguration
{
    HttpServicePointConnectionLimit = 0   // 0 = Abot does not set a limit
};
```

### 5.2 `MaxPagesToCrawl` is now exact

In 2.x, multi-threaded crawls could crawl a few pages more than `MaxPagesToCrawl` (for example 25 to 28 pages with a limit of 25). 3.0 never exceeds the limit. If you compensated for the overshoot, remove the workaround.

### 5.3 Errors during a crawl no longer crash the process

In 2.x, an exception thrown while getting or processing a page could escape the crawler's worker threads and terminate the whole process. This included cancelling a crawl at the wrong moment, a custom scheduler whose `GetNext()` throws, and a `ProcessPage` or `AddPageToContext` override that throws. In 3.0 these are handled the same way the crawler handles its own errors:

- The exception is stored in `CrawlResult.ErrorException`, and `CrawlResult.ErrorOccurred` is `true`.
- The error is logged.
- The crawl is hard-stopped.

Cancellation is still reported as before: `ErrorOccurred` is `true` and `ErrorException` is an `OperationCanceledException`.

The same now applies to exceptions while a page's links or redirect are being scheduled, for example from your `ICrawlDecisionMaker` or your scheduler. In 2.x they were swallowed without a trace, and the link or redirect was silently dropped. Only links and redirect targets that are not valid URIs are still skipped without an error; they are now logged as a warning.

```csharp
var result = await crawler.CrawlAsync(new Uri("https://example.com/"));

if (result.ErrorOccurred)
    Console.WriteLine($"Crawl stopped early: {result.ErrorException.Message}");
```

### 5.4 `AddPageToContext` is called before `ProcessPage`

`WebCrawler` now calls `AddPageToContext` when a worker takes the next page from the scheduler, and then calls `ProcessPage`. In 2.x, `ProcessPage` called `AddPageToContext` itself. If you override `ProcessPage` without calling the base implementation, `AddPageToContext` is now still called for every page; don't call it again from your override.

### 5.5 Very deeply nested HTML is flattened in `AngleSharpHtmlDocument`

Several AngleSharp 1.x members walk the document tree recursively, one call per nesting level, among them `TextContent`, `GetElementsByTagName`, `Clone` and `Normalize`. A malformed or hostile page with thousands of unclosed tags could make them throw a `StackOverflowException`, which .NET cannot catch and which kills the process. `CrawledPage.AngleSharpHtmlDocument` now caps nesting at `CrawledPage.MaxHtmlDocumentDepth` (512 levels, the same limit Chrome uses). Deeper elements are moved up into siblings in document order, so no text, elements or links are lost. Pages within the limit are not changed. `CrawledPage.Content.Text` always contains the original HTML.

### 5.6 `DomainRateLimiter` is disposable and shares limits correctly

`DomainRateLimiter` now implements `IDisposable`; after `Dispose` its methods throw `ObjectDisposedException`. If you create one yourself, dispose it. When two threads added the same domain at the same time, 2.x could give one of them a separate limiter, so that domain was not rate limited correctly. 3.0 always keeps one schedule per domain.

It also no longer blocks threads: `RateLimitAsync` waits asynchronously until the domain's minimum delay since the previous request has passed, and can be cancelled. The delays are unchanged, including the 20 ms that is added to `MinCrawlDelayPerDomainMilliSeconds`. See [section 6.5](#65-classes-that-derive-from-domainratelimiter) if you derive from it.

### 5.7 Fixes you get automatically

These need no action, but may change what you observe:

- `PageRequester` creates exactly one `HttpClient`, even when the first requests run at the same time. 2.x could create several and leak all but one.
- A link found on several pages that are crawled at the same time is evaluated once. In 2.x, `ShouldCrawlPage` (and your `ShouldScheduleLinkDecisionMaker`) could be called for it more than once, and `PageCrawlDisallowed` could fire more than once for the same link.
- `PoliteWebCrawler` disposes the rate limiter and the robots.txt `PageRequester` it creates.
- The crawl timeout no longer uses a timer that could outlive the crawl, and `TaskThreadManager` disposes the cancellation token source it creates.
- When a redirect is not followed automatically, a redirect target that is already known (crawled, scheduled or evaluated before) is skipped without being evaluated again. 2.x called `ShouldCrawlPage` for it, and could fire `PageCrawlDisallowed` for it, before dropping it as a duplicate.
- A `RateLimiter` timer callback that runs after `Dispose` no longer crashes the process.
- `RateLimiter` measures waits precisely on every platform. On .NET Framework, 2.x used `Environment.TickCount`, which only advances every ~15 ms, so waits could end up to ~15 ms early.
- A `RateLimiter` with a time unit of zero no longer keeps a thread busy. In 2.x its timer rescheduled itself with no delay for as long as the limiter existed.
- `CompactCrawledUrlRepository`, the default crawled-URL store, now identifies URLs by a 64-bit hash that is the same in every process. 2.x used `Uri.GetHashCode()`: only 32 bits, so different URLs could share a hash (about 100 pairs per million URLs) and the second page was skipped as already crawled. On .NET Core and later that hash is also seeded randomly per process, so which page was skipped changed from run to run. Which URLs count as duplicates is unchanged.
- `BloomFilter<string>` (used by `BloomFilterCrawledUrlRepository`) no longer uses `string.GetHashCode()`, so its results are the same on every run.
- When a page declares a charset that .NET doesn't know, the warning now includes the charset. 2.x logged a broken message template instead.
- When the crawler follows redirects itself (`IsHttpRequestAutoRedirectsEnabled = false`), a redirect response without a `Location` header is logged as a warning and not followed. 2.x treated it as a redirect to the site's base URI. For example, it crawled `http://a.com/` again when recrawling was enabled, or made it the new crawl root when the root page answered that way.

### 5.8 Abot can run on a clock you control

Everything in Abot that waits or reads the time can take a .NET `TimeProvider`: the crawl timeout, the politeness delay, the retry delay, `CachedMemoryMonitor`'s refresh, and the timestamps on `CrawlContext` and `CrawledPage`. These constructors have a new optional `timeProvider` parameter:

- `PoliteWebCrawler` (the constructor without a thread manager) and `WebCrawler` (protected, for subclasses)
- `PageRequester`, `CachedMemoryMonitor` and `DomainRateLimiter`
- `RateLimiter` (obsolete, see [section 6.5](#65-classes-that-derive-from-domainratelimiter))

If you pass nothing, Abot uses the system clock, so nothing changes. Passing a fake clock lets you test code that uses Abot without real waiting, for example with Microsoft's `FakeTimeProvider` (package `Microsoft.Extensions.TimeProvider.Testing`, for .NET 8 and later and .NET Framework 4.6.2 and later):

```csharp
var time = new FakeTimeProvider();
using var rateLimiter = new DomainRateLimiter(1000, time);   // 1000 ms + Abot's 20 ms margin per domain
var page = new Uri("https://example.com/");

await rateLimiter.RateLimitAsync(page);           // the first request to a domain proceeds at once
var second = rateLimiter.RateLimitAsync(page);    // the next one waits for the delay
time.Advance(TimeSpan.FromMilliseconds(1019));    // second.IsCompleted is still false
time.Advance(TimeSpan.FromMilliseconds(1));
await second;                                     // completes exactly when the delay has passed
```

A crawler built with a fake clock only times out, finishes politeness and retry waits, and moves its timestamps when you advance the clock.

### 5.9 `Constants.LogFormatTemplate` is read-only

`Constants.LogFormatTemplate` is now `static readonly`. Reading it works as before. Code that assigns it no longer compiles; use your own template string instead.

### 5.10 Requests can run without a timeout

`CrawlConfiguration.HttpRequestTimeoutInSeconds` can now be set to `-1` (`CrawlConfiguration.NoHttpRequestTimeout`) so that requests never time out. Use it, for example, when crawls are bounded by `CrawlTimeoutSeconds` or cancellation instead.

The other values work as in 2.x: a positive value is the timeout in seconds (15 by default), and `0` leaves `HttpClient`'s own default of 100 seconds. Values below `-1` are now rejected: creating a `PageRequester` throws `ArgumentOutOfRangeException`. This includes the one the crawler creates when you don't pass your own. As before, the setting only applies to the `HttpClient` Abot creates, not to one you pass in.

```csharp
var config = new CrawlConfiguration
{
    HttpRequestTimeoutInSeconds = CrawlConfiguration.NoHttpRequestTimeout,   // -1: no timeout per request
    CrawlTimeoutSeconds = 600                                                // the whole crawl still stops after 10 minutes
};
```

## 6. Async crawl engine and cancellation

### 6.1 Pages are processed by async workers; thread managers are obsolete

The crawler no longer runs pages through an `IThreadManager`. It starts `CrawlConfiguration.MaxConcurrentThreads` async workers (`Environment.ProcessorCount` if it is 0 or less). Each worker takes the next page and processes it completely: the politeness and retry waits, the request, parsing, scheduling its links and firing its events. Then it takes the next one. Waiting does not block a thread.

This changes what you can observe:

- **`MaxConcurrentThreads` is now a real limit.** In 2.x each page gave its thread back at its first `await`, so many more requests than `MaxConcurrentThreads` could be in flight at once. In 3.0 at most `MaxConcurrentThreads` pages are processed at the same time, including pages waiting for the politeness or retry delay. If crawls of many domains become slower, raise `MaxConcurrentThreads`.
- **Crawls finish as soon as the work is done.** 2.x checked for remaining work every 2.5 seconds, so every crawl took up to 2.5 seconds longer than needed. A stop, hard stop, cancellation or timeout now also takes effect straight away.
- **No threads are blocked while waiting**, so many crawls can run in one process without starving the thread pool.

`IThreadManager`, `ThreadManager`, `TaskThreadManager` and `ManualThreadManager` are obsolete, and so are the `WebCrawler` and `PoliteWebCrawler` constructors that take an `IThreadManager`. Those constructors still work: the thread manager's `MaxThreads` sets how many pages are processed at the same time, but the crawler doesn't use it otherwise. It never calls `DoWork` or `AbortAll` on it and doesn't dispose it. The thread manager types themselves still work as before, for code that uses them directly.

**2.x**:

```csharp
using var threadManager = new TaskThreadManager(20);
using var crawler = new PoliteWebCrawler(config, null, threadManager, null, null, null, null, null, null);
```

**3.0**:

```csharp
config.MaxConcurrentThreads = 20;
using var crawler = new PoliteWebCrawler(config);
```

If you used a thread manager for your own work, use `Task.Run` with `SemaphoreSlim.WaitAsync`, or `Parallel.ForEachAsync` on .NET 6 and later.

### 6.2 `CrawlAsync` takes a `CancellationToken`

`CrawlAsync(Uri uri, CancellationToken cancellationToken = default)` replaces `CrawlAsync(Uri)` and `CrawlAsync(Uri, CancellationTokenSource)`. The `CancellationTokenSource` overload is obsolete but still works.

**2.x**:

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
var result = await crawler.CrawlAsync(new Uri("https://example.com/"), cancellation);
```

**3.0**:

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
var result = await crawler.CrawlAsync(new Uri("https://example.com/"), cancellation.Token);
```

A cancellation is reported as before: `CrawlResult.ErrorOccurred` is `true`, `ErrorException` is an `OperationCanceledException`, and `CrawlContext.IsCrawlHardStopRequested` is `true`.

What is new is that a cancellation, a hard stop (`CrawlContext.IsCrawlHardStopRequested = true`) or `CrawlTimeoutSeconds` now also **cancels the requests and waits in progress**. In 2.x they let pages in progress finish their request first. A page whose request is cancelled is not completed, so `PageCrawlCompleted` does not fire for it. A plain stop (`IsCrawlStopRequested`) still lets the pages in progress finish.

`CrawlContext.CancellationTokenSource` is now a source that the crawler creates for each crawl and links to your token. Cancelling it from an event handler or a decision delegate still cancels the crawl, as in 2.x. It is disposed when the crawl ends, so don't keep it for later.

### 6.3 Interface changes

If you implement any of these interfaces, update your implementation. Code that only calls them compiles unchanged, because the new parameters are optional. Assemblies compiled against 2.x must be recompiled.

| Interface | 2.x | 3.0 |
|---|---|---|
| `IWebCrawler` | `CrawlAsync(Uri)`, `CrawlAsync(Uri, CancellationTokenSource)` | `CrawlAsync(Uri, CancellationToken = default)`; the `CancellationTokenSource` overload is obsolete |
| `IPageRequester` | `MakeRequestAsync(Uri)`, `MakeRequestAsync(Uri, Func<…>)` | both take a last `CancellationToken cancellationToken = default` |
| `IWebContentExtractor` | `GetContentAsync(HttpResponseMessage)` | `GetContentAsync(HttpResponseMessage, CancellationToken = default)` |
| `IRobotsDotTextFinder` | `FindAsync(Uri)` | `FindAsync(Uri, CancellationToken = default)` |
| `IDomainRateLimiter` | `RateLimit(Uri)` | new `RateLimitAsync(Uri, CancellationToken = default)`; `RateLimit` is obsolete |
| `IScheduler` | `void AddKnownUri(Uri)` | `bool AddKnownUri(Uri)`, and new `AddKnownPage(PageToCrawl)` |

Honour the token: the crawler cancels it on cancellation, hard stop and timeout, and a request that ignores it keeps the crawl waiting until it finishes. When the token is cancelled, throw an `OperationCanceledException`, for example with `cancellationToken.ThrowIfCancellationRequested()` or by passing the token on to `HttpClient`, instead of returning a page with an error.

`IScheduler` changed so that the crawler can schedule links without locks:

- `AddKnownUri(Uri)` marks a URI as known and returns `true` only if it wasn't known yet. It must be atomic: when two threads add the same URI at the same time, exactly one of them gets `true`.
- `AddKnownPage(PageToCrawl)` schedules a page whose URI the crawler has already marked as known, without checking for duplicates again.

```csharp
public class MyScheduler : IScheduler
{
    private readonly ConcurrentDictionary<string, byte> _known = new ConcurrentDictionary<string, byte>();
    private readonly ConcurrentQueue<PageToCrawl> _pages = new ConcurrentQueue<PageToCrawl>();

    public bool AddKnownUri(Uri uri) => _known.TryAdd(uri.AbsoluteUri, 0);

    public void AddKnownPage(PageToCrawl page) => _pages.Enqueue(page);

    public void Add(PageToCrawl page)
    {
        if (page.IsRetry || AddKnownUri(page.Uri))
            _pages.Enqueue(page);
    }

    // Count, GetNext, Clear, IsUriKnown, Add(IEnumerable<PageToCrawl>) and Dispose as before
}
```

### 6.4 Classes that derive from `WebCrawler`

The protected methods the crawler calls now take a `CancellationToken`. The old overloads are obsolete, and **the crawler no longer calls them**, so an override of an old overload is silently skipped. The compiler warns about each one (`CS0672: Member overrides obsolete member`). Move the code to the new overload:

| Override in 2.x | Override in 3.0 |
|---|---|
| `CrawlAsync(Uri, CancellationTokenSource)` | `CrawlAsync(Uri, CancellationToken)` |
| `CrawlSite()` | `CrawlSite(CancellationToken)` |
| `ProcessPage(PageToCrawl)` | `ProcessPage(PageToCrawl, CancellationToken)` |
| `CrawlThePage(PageToCrawl)` | `CrawlThePage(PageToCrawl, CancellationToken)` |
| `WaitMinimumRetryDelay(PageToCrawl)` (blocking) | `WaitMinimumRetryDelayAsync(PageToCrawl, CancellationToken)` |

**2.x**:

```csharp
protected override async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl)
{
    _log.Add(pageToCrawl.Uri);
    return await base.CrawlThePage(pageToCrawl);
}
```

**3.0**:

```csharp
protected override async Task<CrawledPage> CrawlThePage(PageToCrawl pageToCrawl, CancellationToken cancellationToken)
{
    _log.Add(pageToCrawl.Uri);
    return await base.CrawlThePage(pageToCrawl, cancellationToken);
}
```

Other changes for subclasses:

- **New `BeforeRequestAsync(PageToCrawl, CancellationToken)`** runs before each request, after `PageCrawlStarting` has fired. `PoliteWebCrawler` overrides it to wait for the domain's politeness delay, so if you override it in a subclass of `PoliteWebCrawler`, call the base implementation.
- **New `SignalWorkChanged()`**: the workers wait for a signal instead of polling. If your subclass adds pages to the scheduler outside `SchedulePageLinks`, `ProcessRedirect` or a retry, call `SignalWorkChanged()` afterwards, so idle workers pick them up.
- **New `TimeProvider` property and `Now()` method**: use them instead of `DateTime.Now` so that your subclass follows the crawler's clock ([section 5.8](#58-abot-can-run-on-a-clock-you-control)).
- **Constructors**: the protected `WebCrawler` constructor without a thread manager takes an optional `TimeProvider`. The constructor with an `IThreadManager` is obsolete ([section 6.1](#61-pages-are-processed-by-async-workers-thread-managers-are-obsolete)).
- **Obsolete fields**: `_threadManager` holds the thread manager passed to an obsolete constructor, and is not used. `_timeoutTimer` is always `null`, because the crawl timeout is now a cancellation token.

### 6.5 Classes that derive from `DomainRateLimiter`

`DomainRateLimiter` no longer keeps an `IRateLimiter` per domain. The protected field `_rateLimiterLookup` and the protected method `GetRateLimiter(Uri, long)` are removed. They are replaced by `GetDomainSchedule(Uri, long)`, which returns the domain's `DomainSchedule` (its delay and the earliest time its next request may start). Override `GetDomainSchedule` if you need to change how a domain's delay is chosen.

`DomainRateLimiter` also has a new public `GetMinCrawlDelay(Uri)`, which returns the delay used for a domain, or `null` if it has none.

`IRateLimiter` and `RateLimiter` are obsolete, because Abot no longer uses them. They still work as before. For general-purpose rate limiting, use `System.Threading.RateLimiting`.

## 7. Package changes

- The package includes XML documentation (`Abot2.xml`), so IntelliSense shows descriptions of Abot's types and members.
- The package icon is embedded in the package. The old icon URL no longer worked.

## Upgrade checklist

1. Update the package reference to 3.0 and build.
2. Fix any `CS0506` errors by changing `Dispose()` overrides to `Dispose(bool)` overrides ([section 2](#2-dispose-is-no-longer-virtual-override-disposebool)).
3. Rename references to `ThreadManager._isDisplosed` to `_isDisposed`.
4. For every object you pass into an Abot constructor, make sure you dispose it after the crawler ([section 1](#1-abot-no-longer-disposes-objects-you-pass-in)).
5. If you pass in your own `HttpClient`, configure its timeout, user agent and handler settings yourself.
6. On .NET 8 or later, decide whether the default limit of 200 connections per host suits you ([section 5.1](#51-httpservicepointconnectionlimit-now-works-on-net-8-and-later)).
7. Update Serilog sinks and enrichers to versions that support Serilog 4, and check direct use of AngleSharp APIs against AngleSharp 1.x.
8. Replace `catch (InvalidOperationException)` around `ThreadManager.DoWork` with `catch (ObjectDisposedException)` if you used it to detect a disposed thread manager.
9. Pass a `CancellationToken` to `CrawlAsync` instead of a `CancellationTokenSource` ([section 6.2](#62-crawlasync-takes-a-cancellationtoken)).
10. Replace constructors that take a thread manager with the ones without, and set `CrawlConfiguration.MaxConcurrentThreads` instead. Check that it allows enough pages at the same time, because it is now enforced ([section 6.1](#61-pages-are-processed-by-async-workers-thread-managers-are-obsolete)).
11. If you implement Abot's interfaces, add the `CancellationToken` parameters and honour them, and update `IScheduler` implementations for `bool AddKnownUri` and `AddKnownPage` ([section 6.3](#63-interface-changes)).
12. If you derive from `WebCrawler`, fix every `CS0672` warning by overriding the `CancellationToken` overload instead: the crawler no longer calls the old ones ([section 6.4](#64-classes-that-derive-from-webcrawler)). If you derive from `DomainRateLimiter`, move `GetRateLimiter` overrides to `GetDomainSchedule` ([section 6.5](#65-classes-that-derive-from-domainratelimiter)).
13. Recompile everything that references Abot, because method signatures changed.
14. If your crawl decision maker or scheduler can throw while links are scheduled, expect the crawl to stop with that error instead of silently dropping the link ([section 5.3](#53-errors-during-a-crawl-no-longer-crash-the-process)).
15. On .NET Framework, set `HttpServicePointConnectionLimit` to `0` if your application sets `ServicePointManager.DefaultConnectionLimit` itself ([section 5.1](#51-httpservicepointconnectionlimit-now-works-on-net-8-and-later)).
