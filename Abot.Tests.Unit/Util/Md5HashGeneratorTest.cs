using Abot.Util;

namespace Abot.Tests.Unit.Util
{
    public class Md5HashGeneratorTest : HashGeneratorTest
    {
        protected override IHashGenerator GetInstance()
        {
            return new Md5HashGenerator();
        }
    }
}
