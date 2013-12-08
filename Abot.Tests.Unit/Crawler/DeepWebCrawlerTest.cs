using Abot.Core;
using Abot.Crawler;
using Commoner.Core.Testing;
using Moq;
using NUnit.Framework;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class DeepWebCrawlerTest
    {
        [Test]
        public void Constructor_NullScheduler_CreatesDeepCrawlerOptmizedScheduler()
        {
            DeepWebCrawler crawler = new DeepWebCrawler();

            IScheduler setScheduler = ValueHelper.GetFieldValue(crawler, "_scheduler") as IScheduler;
            Assert.IsNotNull(setScheduler);
            Assert.IsTrue(setScheduler is Scheduler);

            ICrawledUrlRepository setCrawledUrlRepo = ValueHelper.GetFieldValue(setScheduler, "_crawledUrlRepo") as ICrawledUrlRepository;
            Assert.IsNotNull(setCrawledUrlRepo);
            Assert.IsTrue(setCrawledUrlRepo is OnDiskCrawledUrlRepository);

            IPagesToCrawlRepository setPagesToCrawlRepo = ValueHelper.GetFieldValue(setScheduler, "_pagesToCrawlRepo") as IPagesToCrawlRepository;
            Assert.IsNotNull(setPagesToCrawlRepo);
            Assert.IsTrue(setPagesToCrawlRepo is FifoPagesToCrawlRepository);
        }

        [Test]
        public void Constructor_NonNullScheduler_DoesNotOptimizeSchedulerForDeepCrawling()
        {
            Mock<IScheduler> fakeScheduler = new Mock<IScheduler>();
            DeepWebCrawler crawler = new DeepWebCrawler(null, null, null, fakeScheduler.Object, null, null, null, null, null);
            
            IScheduler setScheduler = ValueHelper.GetFieldValue(crawler, "_scheduler") as IScheduler;
            Assert.IsNotNull(setScheduler);
            Assert.AreSame(fakeScheduler.Object, setScheduler);
        }

    }
}
