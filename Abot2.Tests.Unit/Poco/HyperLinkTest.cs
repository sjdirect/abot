using Abot2.Poco;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abot2.Tests.Unit.Poco
{
    [TestClass]
    public class HyperLinkTest
    {
        HyperLink yahoo1 = new HyperLink { HrefValue = new Uri("http://yahoo.com") };
        HyperLink yahoo2 = new HyperLink { HrefValue = new Uri("http://yahoo.com") };
        HyperLink yahoo3 = new HyperLink { HrefValue = new Uri("http://yahoo3.com") };

        [TestMethod]
        public void Equals_ReturnsTrueWhenEqual()
        {
            Assert.IsTrue(yahoo1.Equals(yahoo2));
            Assert.IsTrue(yahoo2.Equals(yahoo1));
            Assert.IsTrue(object.Equals(yahoo1, yahoo2));
            Assert.IsTrue(object.Equals(yahoo2, yahoo1));
            Assert.AreEqual(yahoo1, yahoo2);
            Assert.AreEqual(yahoo2, yahoo1);
        }

        [TestMethod]
        public void Equals_ReturnsFalseWhenNotEqual()
        {
            Assert.IsFalse(yahoo1.Equals(yahoo3));
            Assert.IsFalse(yahoo3.Equals(yahoo1));
            Assert.IsFalse(object.Equals(yahoo1, yahoo3));
            Assert.IsFalse(object.Equals(yahoo3, yahoo1));
            Assert.AreNotEqual(yahoo1, yahoo3);
            Assert.AreNotEqual(yahoo3, yahoo1);
        }

        [TestMethod]
        public void GetHashCode_ReturnsUriHashcode()
        {
            Assert.AreEqual(yahoo1.HrefValue.GetHashCode(), yahoo1.GetHashCode());
            Assert.AreEqual(yahoo3.HrefValue.GetHashCode(), yahoo3.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_NullHrefValue_DoesNotThrowException()
        {
            new HyperLink().GetHashCode();
        }
    }
}
