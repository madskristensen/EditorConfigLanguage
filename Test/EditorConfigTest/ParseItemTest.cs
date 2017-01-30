using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class ParseItemTest
    {
        [TestMethod]
        public void Equals()
        {
            var a = new ParseItem(default(EditorConfigDocument), ItemType.Keyword, new Span(10, 10), "a text");
            var b = new ParseItem(default(EditorConfigDocument), ItemType.Section, new Span(20, 10), "b text");
            var c = new ParseItem(default(EditorConfigDocument), ItemType.Keyword, new Span(10, 10), "a text");

            Assert.IsTrue(a == c);
            Assert.IsFalse(a != c);
            Assert.IsTrue(a != b);
            Assert.IsFalse(a == b);
            Assert.IsTrue(a.GetHashCode() != b.GetHashCode());
            Assert.IsTrue(a.Equals(a));
            Assert.IsTrue(a.Equals(c));
            Assert.IsFalse(a.Equals(b));
        }
    }
}
