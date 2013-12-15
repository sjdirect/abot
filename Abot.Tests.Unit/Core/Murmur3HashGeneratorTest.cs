using Abot.Core;

namespace Abot.Tests.Unit.Core
{
    public class Murmur3HashGeneratorTest : HashGeneratorTest
    {
        protected override IHashGenerator GetInstance()
        {
            return new Murmur3HashGenerator();
        }
    }
}
