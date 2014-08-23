using Abot.Util;

namespace Abot.Tests.Unit.Util
{
    public class Murmur3HashGeneratorTest : HashGeneratorTest
    {
        protected override IHashGenerator GetInstance()
        {
            return new Murmur3HashGenerator();
        }
    }
}
