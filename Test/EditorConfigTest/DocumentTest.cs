using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class DocumentTest
    {
        [TestMethod, TestCategory("MEF")]
        public async Task Parse()
        {
            ITextBuffer buffer = Mef.CreateTextBuffer(Samples.OneSectionStandard);
            var doc = EditorConfigDocument.FromTextBuffer(buffer);

            await doc.WaitForParsingComplete();

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
        }

        [TestMethod, TestCategory("MEF")]
        public async Task MultipleValues()
        {
            ITextBuffer buffer = Mef.CreateTextBuffer(Samples.MultipleValuesSection);
            var doc = EditorConfigDocument.FromTextBuffer(buffer);

            await doc.WaitForParsingComplete();

            Assert.AreEqual(3, doc.ParseItems.Count);
            Assert.AreEqual("accessors, indexers", doc.ParseItems.Last().Text);
        }

        [TestMethod, TestCategory("MEF")]
        public async Task Suppressions()
        {
            ITextBuffer buffer = Mef.CreateTextBuffer(Samples.Suppression);
            var doc = EditorConfigDocument.FromTextBuffer(buffer);

            await doc.WaitForParsingComplete();

            Assert.AreEqual(3, doc.ParseItems.Count);
            Assert.AreEqual("EC101", doc.ParseItems[1].Text);
            Assert.AreEqual(12, doc.ParseItems[1].Span.Start);
            Assert.AreEqual(5, doc.ParseItems[1].Span.Length);
        }

        [TestMethod]
        public void NamingRules()
        {
            var file = new FileInfo(@"..\..\..\..\src\schema\EditorConfig.json");
            SchemaCatalog.ParseJson(file.FullName);

            bool exist = SchemaCatalog.TryGetKeyword("dotnet_naming_rule.foo.symbols", out Keyword keyword);

            Assert.IsTrue(exist);
        }
    }
}
