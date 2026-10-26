using System.Threading.Tasks;
using System.Linq;
using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Moq;

namespace EditorConfigTest
{
    [TestClass]
    public class ClassifierTest
    {
        [TestMethod]
        [DataRow("[*]\r\nindent_style = spaces", "[*]", "indent_style", "spaces")]
        [DataRow("[*.cs]\r\nindent_style = space", "[*.cs]", "indent_style", "space")]
        [DataRow("[*]\r\nindent_styles = space", "[*]", "indent_styles", "space")]
        [DataRow("\r\n[*]\r\nindent_style = space", "[*]", "indent_style", "space")]
        [DataRow("[*]\r\nindent_style = spac", "[*]", "indent_style", "spac")]
        public async Task EditKeepsColorsBeforeParsingCompletes(string text, string section, string keyword, string value)
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer("[*]\r\nindent_style = space");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            var registry = new Mock<IClassificationTypeRegistryService>();
            registry.Setup(service => service.GetClassificationType(It.IsAny<string>()))
                .Returns((string name) => Mock.Of<IClassificationType>(type => type.Classification == name));
            var classifier = new EditorConfigClassifier(registry.Object, buffer);

            ITextSnapshot snapshot = TestTextBufferFactory.SetText(buffer, text);
            Assert.IsTrue(document.IsParsing);
            var colors = classifier.GetClassificationSpans(new SnapshotSpan(snapshot, 0, snapshot.Length));
            Assert.HasCount(3, colors);
            Assert.AreEqual(section, colors[0].Span.GetText().Trim());
            Assert.AreEqual("string", colors[0].ClassificationType.Classification);
            Assert.AreEqual(keyword, colors[1].Span.GetText());
            Assert.AreEqual("identifier", colors[1].ClassificationType.Classification);
            Assert.AreEqual(value, colors[2].Span.GetText());
            Assert.AreEqual("keyword", colors[2].ClassificationType.Classification);
            Assert.IsTrue(colors.All(color => color.Span.Snapshot == snapshot));

            await document.WaitForParsingCompleteAsync();
            var parsedColors = classifier.GetClassificationSpans(new SnapshotSpan(snapshot, 0, snapshot.Length));
            CollectionAssert.AreEqual(new[] { section, keyword, value }, parsedColors.Select(color => color.Span.GetText()).ToArray());
            Assert.AreEqual(text.IndexOf(keyword, System.StringComparison.Ordinal), colors[1].Span.Start.Position);
        }

        [TestMethod]
        public async Task RapidEditsKeepColorsAndCompletedParseReplacesOldTokenTypes()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer("indent_style = space");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            var registry = new Mock<IClassificationTypeRegistryService>();
            registry.Setup(service => service.GetClassificationType(It.IsAny<string>()))
                .Returns((string name) => Mock.Of<IClassificationType>(type => type.Classification == name));
            var classifier = new EditorConfigClassifier(registry.Object, buffer);

            foreach (string text in new[] { "indent_style = spaces", "indent_style = spacesx", "#indent_style = spacesx" })
            {
                ITextSnapshot snapshot = TestTextBufferFactory.SetText(buffer, text);
                Assert.IsTrue(document.IsParsing);
                var colors = classifier.GetClassificationSpans(new SnapshotSpan(snapshot, 0, snapshot.Length));
                Assert.HasCount(2, colors);
                Assert.AreEqual("identifier", colors[0].ClassificationType.Classification);
                Assert.AreEqual(text.Substring(text.IndexOf('=') + 2), colors[1].Span.GetText());
                Assert.AreEqual("keyword", colors[1].ClassificationType.Classification);
            }

            await document.WaitForParsingCompleteAsync();
            ITextSnapshot latest = buffer.CurrentSnapshot;
            var parsedColors = classifier.GetClassificationSpans(new SnapshotSpan(latest, 0, latest.Length));
            Assert.HasCount(1, parsedColors);
            Assert.AreEqual("comment", parsedColors[0].ClassificationType.Classification);
            Assert.AreEqual(latest.GetText(), parsedColors[0].Span.GetText());

            ITextSnapshot empty = TestTextBufferFactory.SetText(buffer, "");
            Assert.IsTrue(document.IsParsing);
            Assert.HasCount(0, classifier.GetClassificationSpans(new SnapshotSpan(empty, 0, 0)));
            await document.WaitForParsingCompleteAsync();
            Assert.HasCount(0, classifier.GetClassificationSpans(new SnapshotSpan(empty, 0, 0)));
        }

        [TestMethod]
        public async Task EmptyDocumentHasNoClassificationSpans()
        {
            ITextBuffer buffer = TestTextBufferFactory.CreateTextBuffer("");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            var registry = new Mock<IClassificationTypeRegistryService>();
            registry.Setup(service => service.GetClassificationType(It.IsAny<string>()))
                .Returns((string name) => Mock.Of<IClassificationType>(type => type.Classification == name));
            var classifier = new EditorConfigClassifier(registry.Object, buffer);
            Assert.HasCount(0, classifier.GetClassificationSpans(new SnapshotSpan(buffer.CurrentSnapshot, 0, 0)));
        }
    }
}