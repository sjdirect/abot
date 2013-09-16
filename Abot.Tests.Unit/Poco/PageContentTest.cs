using Abot.Poco;
using NUnit.Framework;

namespace Abot.Tests.Unit.Poco
{
    [TestFixture]
    public class PageContentTest
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            PageContent unitUnderTest = new PageContent();
            Assert.IsNull(unitUnderTest.Bytes);
            Assert.IsNull(unitUnderTest.Charset);
            Assert.IsNull(unitUnderTest.Encoding);
            Assert.AreEqual("", unitUnderTest.Text);
        }        
    }
}
