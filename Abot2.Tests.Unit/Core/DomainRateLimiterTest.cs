﻿using Abot2.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit.Core
{
    //Waits run on a manual clock that only moves when a test advances it, so results never depend on timing.
    //RateLimitAsync returns a completed task when no wait is needed, and a task that completes exactly when the delay has passed otherwise.
    [TestClass]
    public class DomainRateLimiterTest
    {
        readonly Uri _uri = new Uri("http://a.com/");
        ManualTimeProvider _time;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetUp()
        {
            _time = new ManualTimeProvider();
        }

        [TestMethod]
        public void Constructor_ZeroCrawlDelay_NoException() => new DomainRateLimiter(0);

        [TestMethod]
        public void Constructor_NegativeCrawlDelay_ThrowsException() => Assert.ThrowsExactly<ArgumentException>(() => new DomainRateLimiter(-1));

        [TestMethod]
        public void RateLimit_SameDomain_WaitsBetweenRequests()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);

            Assert.IsTrue(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken).IsCompleted, "The first request should not wait");

            //100 ms plus the 20 ms margin DomainRateLimiter adds to its default delay
            AssertCompletesAfter(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken), 120);
        }

        [TestMethod]
        public void RateLimit_ConcurrentRequestsToSameDomain_AreSpacedOneDelayApart()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);

            var first = unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            var second = unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            var third = unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);

            Assert.IsTrue(first.IsCompleted);
            AssertCompletesAfter(second, 120);
            AssertCompletesAfter(third, 120);
        }

        [TestMethod]
        public void RateLimit_DelayAlreadyPassed_DoesNotWait()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);

            _time.Advance(TimeSpan.FromMilliseconds(500));

            Assert.IsTrue(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken).IsCompleted);
        }

        [TestMethod]
        public void RateLimit_ZeroAsDefault_SameDomain_DoesNotWaitsBetweenRequests()
        {
            var unitUnderTest = new DomainRateLimiter(0, _time);

            Assert.IsTrue(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken).IsCompleted);
            Assert.IsNull(unitUnderTest.GetMinCrawlDelay(_uri));
        }

        [TestMethod]
        public void RateLimit_DifferentDomain_DoesNotWaitsBetweenRequests()
        {
            var unitUnderTest = new DomainRateLimiter(1000, _time);

            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://a.com/"), TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://b.com/"), TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://c.com/"), TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://d.com/"), TestContext.CancellationToken).IsCompleted);
        }

        [TestMethod]
        public void RateLimit_ZeroAsDefault_DifferentDomain_DoesNotWaitsBetweenRequests()
        {
            var unitUnderTest = new DomainRateLimiter(0, _time);

            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://a.com/"), TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://b.com/"), TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://c.com/"), TestContext.CancellationToken).IsCompleted);
            Assert.IsTrue(unitUnderTest.RateLimitAsync(new Uri("http://d.com/"), TestContext.CancellationToken).IsCompleted);
        }

        [TestMethod]
        public void RateLimit_PagesOfSameDomain_ShareItsDelay()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);

            unitUnderTest.RateLimitAsync(new Uri("http://a.com/a.html"), TestContext.CancellationToken);

            AssertCompletesAfter(unitUnderTest.RateLimitAsync(new Uri("http://a.com/b.html"), TestContext.CancellationToken), 120);
        }

        [TestMethod]
        public async Task RateLimit_Cancelled_CancelsTheWait()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);
            await unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var wait = unitUnderTest.RateLimitAsync(_uri, cancellationTokenSource.Token);
                Assert.IsFalse(wait.IsCompleted);

                cancellationTokenSource.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(() => wait);
            }
        }

        [TestMethod]
        public void RateLimit_NullUri() => Assert.ThrowsExactly<ArgumentNullException>(() => new DomainRateLimiter(1000).RateLimitAsync(null, TestContext.CancellationToken));

        [TestMethod]
        public void AddDomain_NullUri() => Assert.ThrowsExactly<ArgumentNullException>(() => new DomainRateLimiter(1000).AddDomain(null, 100));

        [TestMethod]
        public void AddDomain_ZeroCrawlDelay() => Assert.ThrowsExactly<ArgumentException>(() => new DomainRateLimiter(1000).AddDomain(new Uri("http://a.com"), 0));

        [TestMethod]
        public void AddDomain_NegativeCrawlDelay() => Assert.ThrowsExactly<ArgumentException>(() => new DomainRateLimiter(1000).AddDomain(new Uri("http://a.com"), -1));

        [TestMethod]
        public void AddDomain_ParamLessThanDefault_UsesDefault()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);

            unitUnderTest.AddDomain(_uri, 5);

            Assert.AreEqual(TimeSpan.FromMilliseconds(120), unitUnderTest.GetMinCrawlDelay(_uri));
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            AssertCompletesAfter(unitUnderTest.RateLimitAsync(new Uri("http://a.com/a.html"), TestContext.CancellationToken), 120);
        }

        [TestMethod]
        public void AddDomain_ParamGreaterThanDefault_UsesParam()
        {
            var unitUnderTest = new DomainRateLimiter(5, _time);

            unitUnderTest.AddDomain(_uri, 100);

            Assert.AreEqual(TimeSpan.FromMilliseconds(100), unitUnderTest.GetMinCrawlDelay(_uri));
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            AssertCompletesAfter(unitUnderTest.RateLimitAsync(new Uri("http://a.com/a.html"), TestContext.CancellationToken), 100);
        }

        [TestMethod]
        public void AddDomain_AddDuplicateDomain_FirstAddWins()
        {
            var unitUnderTest = new DomainRateLimiter(5, _time);

            unitUnderTest.AddDomain(_uri, 50);
            unitUnderTest.AddDomain(_uri, 150);//This should be ignored

            Assert.AreEqual(TimeSpan.FromMilliseconds(50), unitUnderTest.GetMinCrawlDelay(_uri));
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            AssertCompletesAfter(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken), 50);
        }

        [TestMethod]
        public void AddOrUpdateDomain_NullUri() => Assert.ThrowsExactly<ArgumentNullException>(() => new DomainRateLimiter(1000).AddOrUpdateDomain(null, 100));

        [TestMethod]
        public void AddOrUpdateDomain_ZeroCrawlDelay() => Assert.ThrowsExactly<ArgumentException>(() => new DomainRateLimiter(1000).AddOrUpdateDomain(new Uri("http://a.com"), 0));

        [TestMethod]
        public void AddOrUpdateDomain_NegativeCrawlDelay() => Assert.ThrowsExactly<ArgumentException>(() => new DomainRateLimiter(1000).AddOrUpdateDomain(new Uri("http://a.com"), -1));

        [TestMethod]
        public void AddOrUpdateDomain_ParamLessThanDefault_UsesDefault()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);

            unitUnderTest.AddOrUpdateDomain(_uri, 5);

            Assert.AreEqual(TimeSpan.FromMilliseconds(120), unitUnderTest.GetMinCrawlDelay(_uri));
        }

        [TestMethod]
        public void AddOrUpdateDomain_ParamGreaterThanDefault_UsesParam()
        {
            var unitUnderTest = new DomainRateLimiter(5, _time);

            unitUnderTest.AddOrUpdateDomain(_uri, 100);

            Assert.AreEqual(TimeSpan.FromMilliseconds(100), unitUnderTest.GetMinCrawlDelay(_uri));
        }

        [TestMethod]
        public void AddOrUpdateDomain_AddDuplicateDomain_LastUpdateWins()
        {
            var unitUnderTest = new DomainRateLimiter(5, _time);

            unitUnderTest.AddOrUpdateDomain(_uri, 50);
            unitUnderTest.AddOrUpdateDomain(_uri, 150);//This should override the previous

            Assert.AreEqual(TimeSpan.FromMilliseconds(150), unitUnderTest.GetMinCrawlDelay(_uri));
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            AssertCompletesAfter(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken), 150);
        }

        [TestMethod]
        public void RemoveDomain_NoLongerRateLimitsThatDomain()
        {
            var unitUnderTest = new DomainRateLimiter(5, _time);
            unitUnderTest.AddDomain(_uri, 1000);

            unitUnderTest.RemoveDomain(_uri);

            Assert.IsNull(unitUnderTest.GetMinCrawlDelay(_uri));

            //Rate limiting the domain again falls back to the default delay (5 ms plus the 20 ms margin), not the removed 1000 ms
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);
            AssertCompletesAfter(unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken), 25);
        }

        [TestMethod]
        public void Dispose_ThenRateLimit_ThrowsObjectDisposedException()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);
            unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken);

            unitUnderTest.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => unitUnderTest.RateLimitAsync(_uri, TestContext.CancellationToken));
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var unitUnderTest = new DomainRateLimiter(100, _time);
            unitUnderTest.AddDomain(_uri, 100);
            unitUnderTest.AddOrUpdateDomain(_uri, 200);
            unitUnderTest.RemoveDomain(_uri);

            unitUnderTest.Dispose();
            unitUnderTest.Dispose();
        }

        //Still waiting 1 ms before the delay has passed, completed once it has
        void AssertCompletesAfter(Task wait, int milliseconds)
        {
            Assert.IsFalse(wait.IsCompleted, "Should still be waiting");

            _time.Advance(TimeSpan.FromMilliseconds(milliseconds - 1));
            Assert.IsFalse(wait.IsCompleted, $"Should still be waiting 1 ms before {milliseconds} ms");

            _time.Advance(TimeSpan.FromMilliseconds(1));
            Assert.IsTrue(wait.IsCompleted, $"Should be allowed after {milliseconds} ms");
        }
    }
}
