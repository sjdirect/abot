using Abot.Core;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class DomainRateLimiterTest
    {
        [Test]
        public void Constructor_ZeroCrawlDelay_NoException()
        {
            new DomainRateLimiter(0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NegativeCrawlDelay()
        {
            new DomainRateLimiter(-1);
        }

        [Test]
        public void RateLimit_SameDomain_WaitsBetweenRequests()
        {
            Uri uri = new Uri("http://a.com/");
            Stopwatch timer = Stopwatch.StartNew();
            DomainRateLimiter unitUnderTest = new DomainRateLimiter(100);
            unitUnderTest.RateLimit(uri);
            unitUnderTest.RateLimit(uri);
            unitUnderTest.RateLimit(uri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 175);
        }

        [Test]
        public void RateLimit_ZeroAsDefault_SameDomain_DoesNotWaitsBetweenRequests()
        {
            Uri uri = new Uri("http://a.com/");
            Stopwatch timer = Stopwatch.StartNew();
            DomainRateLimiter unitUnderTest = new DomainRateLimiter(0);
            unitUnderTest.RateLimit(uri);
            unitUnderTest.RateLimit(uri);
            unitUnderTest.RateLimit(uri);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 50);
        }

        [Test]
        public void RateLimit_DifferentDomain_DoesNotWaitsBetweenRequests()
        {
            Uri uri1 = new Uri("http://a.com/");
            Uri uri2 = new Uri("http://b.com/");
            Uri uri3 = new Uri("http://c.com/");
            Uri uri4 = new Uri("http://d.com/");

            Stopwatch timer = Stopwatch.StartNew();
            DomainRateLimiter unitUnderTest = new DomainRateLimiter(1000);
            unitUnderTest.RateLimit(uri1);
            unitUnderTest.RateLimit(uri2);
            unitUnderTest.RateLimit(uri3);
            unitUnderTest.RateLimit(uri4);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 100);
        }

        [Test]
        public void RateLimit_ZeroAsDefault_DifferentDomain_DoesNotWaitsBetweenRequests()
        {
            Uri uri1 = new Uri("http://a.com/");
            Uri uri2 = new Uri("http://b.com/");
            Uri uri3 = new Uri("http://c.com/");
            Uri uri4 = new Uri("http://d.com/");

            Stopwatch timer = Stopwatch.StartNew();
            DomainRateLimiter unitUnderTest = new DomainRateLimiter(0);
            unitUnderTest.RateLimit(uri1);
            unitUnderTest.RateLimit(uri2);
            unitUnderTest.RateLimit(uri3);
            unitUnderTest.RateLimit(uri4);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds < 50);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RateLimit_NullUri()
        {
            new DomainRateLimiter(1000).RateLimit(null);
        }


        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddDomain_NullUri()
        {
            new DomainRateLimiter(1000).AddDomain(null, 100);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddDomain_ZeroCrawlDelay()
        {
            new DomainRateLimiter(1000).AddDomain(new Uri("http://a.com"), 0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddDomain_NegativeCrawlDelay()
        {
            new DomainRateLimiter(1000).AddDomain(new Uri("http://a.com"), -1);
        }

        [Test]
        public void AddDomain_ParamLessThanDefault_UsesDefault()
        {
            Uri rootUri = new Uri("http://a.com/");
            Uri pageUri1 = new Uri("http://a.com/a.html");
            Uri pageUri2 = new Uri("http://a.com/b.html");

            Stopwatch timer = Stopwatch.StartNew();
            DomainRateLimiter unitUnderTest = new DomainRateLimiter(100);

            unitUnderTest.AddDomain(rootUri, 5);

            unitUnderTest.RateLimit(rootUri);
            unitUnderTest.RateLimit(pageUri1);
            unitUnderTest.RateLimit(pageUri2);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 190);
        }

        [Test]
        public void AddDomain_ParamGreaterThanDefault_UsesParam()
        {
            Uri rootUri = new Uri("http://a.com/");
            Uri pageUri1 = new Uri("http://a.com/a.html");
            Uri pageUri2 = new Uri("http://a.com/b.html");

            Stopwatch timer = Stopwatch.StartNew();
            DomainRateLimiter unitUnderTest = new DomainRateLimiter(5);

            unitUnderTest.AddDomain(rootUri, 100);

            unitUnderTest.RateLimit(rootUri);
            unitUnderTest.RateLimit(pageUri1);
            unitUnderTest.RateLimit(pageUri2);
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 190);
        }
    }
}
