using Abot.Core;
using Abot.Crawler;
using Commoner.Core.Testing;
using Moq;
using NUnit.Framework;

namespace Abot.Tests.Unit.Crawler
{
    [TestFixture]
    public class ShallowWebCrawlerTest
    {
        [Test]
        public void Constructor_NullScheduler_CreatesShallowCrawlerOptmizedScheduler()
        {
            ShallowWebCrawler crawler = new ShallowWebCrawler();

            IScheduler setScheduler = ValueHelper.GetFieldValue(crawler, "_scheduler") as IScheduler;
            Assert.IsNotNull(setScheduler);
            Assert.IsTrue(setScheduler is Scheduler);

            ICrawledUrlRepository setCrawledUrlRepo = ValueHelper.GetFieldValue(setScheduler, "_crawledUrlRepo") as ICrawledUrlRepository;
            Assert.IsNotNull(setCrawledUrlRepo);
            Assert.IsTrue(setCrawledUrlRepo is InMemoryCrawledUrlRepository);

            IPagesToCrawlRepository setPagesToCrawlRepo = ValueHelper.GetFieldValue(setScheduler, "_pagesToCrawlRepo") as IPagesToCrawlRepository;
            Assert.IsNotNull(setPagesToCrawlRepo);
            Assert.IsTrue(setPagesToCrawlRepo is InMemoryPagesToCrawlRepository);
        }

        [Test]
        public void Constructor_NonNullScheduler_DoesNotOptimizeSchedulerForShallowCrawling()
        {
            Mock<IScheduler> fakeScheduler = new Mock<IScheduler>();
            ShallowWebCrawler crawler = new ShallowWebCrawler(null, null, null, fakeScheduler.Object, null, null, null, null, null);
            
            IScheduler setScheduler = ValueHelper.GetFieldValue(crawler, "_scheduler") as IScheduler;
            Assert.IsNotNull(setScheduler);
            Assert.AreSame(fakeScheduler.Object, setScheduler);
        }

    }
}
