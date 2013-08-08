using Abot.Core;
using NUnit.Framework;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class ProducerConsumerThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new ProducerConsumerThreadManager(maxThreads);
        }
    }
}
