using Abot.Core;
using NUnit.Framework;
using System.Net;
using System.Threading;

namespace Abot.Tests.Unit.Core
{
    [TestFixture]
    public class TaskThreadManagerTest : ThreadManagerTest
    {
        protected override IThreadManager GetInstance(int maxThreads)
        {
            return new ManualThreadManager(maxThreads);
        }
    }
}
