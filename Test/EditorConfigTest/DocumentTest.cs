using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Moq;

namespace EditorConfigTest
{
    [TestClass]
    public class DocumentTest
    {
        [TestMethod, TestCategory("Parser")]
        public async Task Parse()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer(Samples.OneSectionStandard);
            using var doc = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");

            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(12, doc.ParseItems);
            Assert.AreEqual(ItemType.Keyword, doc.ParseItems[0].ItemType);
            Assert.AreEqual(ItemType.Comment, doc.ParseItems[2].ItemType);

            Property root = doc.Properties[0];
            Assert.HasCount(1, doc.Properties);
            Assert.IsTrue(root.IsValid);
            Assert.AreEqual(SchemaCatalog.Root, root.Keyword.Text);

            Section section = doc.Sections[0];
            Assert.AreEqual("[*.cs]", section.Item.Text);
            Assert.HasCount(4, section.Properties);
            Assert.IsTrue(section.Properties.All(p => p.IsValid));
        }

        [TestMethod, TestCategory("Parser")]
        public async Task MultipleValues()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer(Samples.MultipleValuesSection);
            using var doc = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");

            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(3, doc.ParseItems);
            Assert.AreEqual("accessors, indexers", doc.ParseItems.Last().Text);
        }

        [TestMethod, TestCategory("Parser")]
        public async Task TrailingComment()
        {
            const string source = "indent_size = 4 # comment";
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer(source);
            using var doc = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");

            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(3, doc.ParseItems);
            Assert.AreEqual("4", doc.Properties[0].Value.Text);
            Assert.AreEqual(ItemType.Comment, doc.ParseItems[2].ItemType);
            Assert.AreEqual("# comment", doc.ParseItems[2].Text);
            Assert.AreEqual(source.IndexOf('#'), doc.ParseItems[2].Span.Start);
        }

        [TestMethod, TestCategory("Parser")]
        public async Task Suppressions()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer(Samples.Suppression);
            using var doc = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");

            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(3, doc.ParseItems);
            Assert.AreEqual("EC101", doc.ParseItems[1].Text);
            Assert.AreEqual(12, doc.ParseItems[1].Span.Start);
            Assert.AreEqual(5, doc.ParseItems[1].Span.Length);
        }

        [TestMethod]
        public async Task Parse_PublishesOnlyTheLatestSnapshot()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer("[*]\r\nindent_style = space");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            await document.WaitForParsingCompleteAsync();
            ITextSnapshot original = buffer.CurrentSnapshot;
            TestTextBufferFactory.SetText(buffer, "[*.cs]\r\nindent_size = 4");
            TestTextBufferFactory.SetText(buffer, "[*.vb]\r\nindent_size = 8");
            ITextSnapshot latest = TestTextBufferFactory.SetText(buffer, "[*]\r\nindent_size = 2");
            await document.WaitForParsingCompleteAsync();

            Assert.IsFalse(document.IsParsing);
            Assert.IsFalse(document.TryGetParseResult(original, out _));
            Assert.IsTrue(document.TryGetParseResult(latest, out EditorConfigDocument.ParseResult result));
            Assert.HasCount(3, result.Items);
            Assert.AreEqual("2", result.Sections[0].Properties[0].Value.Text);
            Assert.IsTrue(result.Items.All(item => item.Snapshot == latest));
        }

        [TestMethod]
        public async Task ClosingOneViewKeepsTheSharedParserAlive()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer("[*]\r\nindent_size = 4");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            IWpfTextView first = CreateView(buffer);
            IWpfTextView second = CreateView(buffer);
            VsTextViewCreationListener.RegisterView(first);
            VsTextViewCreationListener.RegisterView(second);

            VsTextViewCreationListener.CloseView(first);
            Assert.AreSame(document, EditorConfigDocument.FromTextBuffer(buffer));
            ITextSnapshot latest = TestTextBufferFactory.SetText(buffer, "[*]\r\nindent_size = 8");
            await document.WaitForParsingCompleteAsync();
            Assert.IsTrue(document.TryGetParseResult(latest, out _));
            Assert.AreSame(second, buffer.Properties.GetProperty<IWpfTextView>(typeof(IWpfTextView)));

            VsTextViewCreationListener.CloseView(second);
            Assert.IsFalse(buffer.Properties.ContainsProperty(typeof(EditorConfigDocument)));
            Assert.IsFalse(buffer.Properties.ContainsProperty(typeof(IWpfTextView)));
        }

        [TestMethod]
        public async Task ClassifierRefreshesColorsForTheMatchingSnapshot()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer("[*]\r\nindent_style = space");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            var registry = new Mock<IClassificationTypeRegistryService>();
            registry.Setup(service => service.GetClassificationType(It.IsAny<string>()))
                .Returns((string name) => Mock.Of<IClassificationType>(type => type.Classification == name));
            var classifier = new EditorConfigClassifier(registry.Object, buffer);
            ITextSnapshot original = buffer.CurrentSnapshot;
            IList<ClassificationSpan> colors = null;
            int notifications = 0;
            classifier.ClassificationChanged += (sender, args) =>
            {
                notifications++;
                colors = classifier.GetClassificationSpans(args.ChangeSpan);
            };

            ITextSnapshot snapshot = TestTextBufferFactory.SetText(buffer, "[*.cs]\r\nindent_size = 2");
            await document.WaitForParsingCompleteAsync();

            Assert.AreEqual(1, notifications);
            Assert.IsNotNull(colors);
            Assert.HasCount(3, colors);
            Assert.AreEqual(new SnapshotSpan(snapshot, 0, 6), colors[0].Span);
            Assert.AreEqual("string", colors[0].ClassificationType.Classification);
            Assert.AreEqual("indent_size", colors[1].Span.GetText());
            Assert.AreEqual("identifier", colors[1].ClassificationType.Classification);
            Assert.AreEqual("2", colors[2].Span.GetText());
            Assert.HasCount(0, classifier.GetClassificationSpans(new SnapshotSpan(original, 0, original.Length)));
            Assert.HasCount(1, classifier.GetClassificationSpans(new SnapshotSpan(snapshot, 1, 1)));

            TestTextBufferFactory.SetText(buffer, "");
            await document.WaitForParsingCompleteAsync();
            Assert.AreEqual(2, notifications);
            Assert.HasCount(0, colors);
        }

        private static IWpfTextView CreateView(ITextBuffer buffer)
        {
            var view = new Mock<IWpfTextView>();
            view.SetupGet(value => value.TextBuffer).Returns(buffer);
            return view.Object;
        }

        [TestMethod]
        public void NamingRules()
        {
            string testDir = Path.GetDirectoryName(typeof(DocumentTest).Assembly.Location);
            string file = Path.Combine(testDir, "schema", "EditorConfig.json");
            SchemaCatalog.ParseJson(file);

            bool exist = SchemaCatalog.TryGetKeyword("dotnet_naming_rule.foo.symbols", out _);

            Assert.IsTrue(exist);
        }
    }
}
