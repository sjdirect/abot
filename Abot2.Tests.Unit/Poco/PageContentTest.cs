using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class PageContentTest
    {
        [TestMethod]
        public void Constructor_CreatesInstance()
        {
            var unitUnderTest = new PageContent();
            Assert.IsNull(unitUnderTest.Bytes);
            Assert.IsNull(unitUnderTest.Charset);
            Assert.IsNull(unitUnderTest.Encoding);
            Assert.AreEqual("", unitUnderTest.Text);
        }        
    }
}
