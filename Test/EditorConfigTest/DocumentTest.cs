using System.Linq;
using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class DocumentTest
    {
        [TestMethod, TestCategory("MEF")]
        public void Parse()
        {
            ITextBuffer buffer = Mef.CreateTextBuffer(_text);
            var doc = EditorConfigDocument.FromTextBuffer(buffer);

            Assert.AreEqual(12, doc.ParseItems.Count);
            Assert.AreEqual(ItemType.Keyword, doc.ParseItems[0].ItemType);
            Assert.AreEqual(ItemType.Comment, doc.ParseItems[2].ItemType);

            Property root = doc.Properties[0];
            Assert.AreEqual(1, doc.Properties.Count);
            Assert.IsTrue(root.IsValid);
            Assert.AreEqual(SchemaCatalog.Root, root.Keyword.Text);

            Section section = doc.Sections[0];
            Assert.AreEqual("[*.cs]", section.Item.Text);
            Assert.AreEqual(4, section.Properties.Count);
            Assert.IsTrue(section.Properties.All(p => p.IsValid));

            bool hasUnknown = doc.ParseItems.Any(p => p.ItemType == ItemType.Unknown);
            Assert.IsFalse(hasUnknown);
        }

        private const string _text = @"root = true

# comment
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true";
    }
}
