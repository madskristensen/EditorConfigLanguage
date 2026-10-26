using System.Threading.Tasks;
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