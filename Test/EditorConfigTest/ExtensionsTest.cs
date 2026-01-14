using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class ExtensionsTest
    {
        [TestMethod]
        public void Is_SameString_ReturnsTrue()
        {
            Assert.IsTrue("hello".Is("hello"));
        }

        [TestMethod]
        public void Is_DifferentCase_ReturnsTrue()
        {
            Assert.IsTrue("HELLO".Is("hello"));
            Assert.IsTrue("hello".Is("HELLO"));
            Assert.IsTrue("HeLLo".Is("hEllO"));
        }

        [TestMethod]
        public void Is_DifferentStrings_ReturnsFalse()
        {
            Assert.IsFalse("hello".Is("world"));
            Assert.IsFalse("hello".Is("hell"));
            Assert.IsFalse("hello".Is("helloo"));
        }

        [TestMethod]
        public void Is_EmptyStrings_ReturnsTrue()
        {
            Assert.IsTrue("".Is(""));
        }

        [TestMethod]
        public void Is_NullComparison_ReturnsFalse()
        {
            Assert.IsFalse("hello".Is(null));
        }

        [TestMethod]
        public void Is_WithWhitespace_ReturnsFalse()
        {
            Assert.IsFalse("hello".Is("hello "));
            Assert.IsFalse(" hello".Is("hello"));
        }
    }
}
