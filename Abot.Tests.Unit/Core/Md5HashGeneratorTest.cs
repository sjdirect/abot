using Abot.Core;

namespace Abot.Tests.Unit.Core
{
    public class Md5HashGeneratorTest : HashGeneratorTest
    {
        protected override IHashGenerator GetInstance()
        {
            return new Md5HashGenerator();
        }
    }
}
