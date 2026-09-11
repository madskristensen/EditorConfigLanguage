using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    [DoNotParallelize]
    public class NamingSymbolServiceTest
    {
        private const string _source = """
            [*.cs]
            dotnet_naming_style.underscored.capitalization = pascal_case
            dotnet_naming_style.underscored.required_prefix = _
            dotnet_naming_rule.private_fields.style = underscored
            """;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            string testDir = Path.GetDirectoryName(typeof(NamingSymbolServiceTest).Assembly.Location);
            SchemaCatalog.ParseJson(Path.Combine(testDir, "schema", "EditorConfig.json"));
        }

        [TestMethod]
        public async Task TryGetSymbolAtPosition_ResolvesDeclarationsAndReferences()
        {
            using EditorConfigDocument document = await CreateDocumentAsync();
            int declarationPosition = _source.IndexOf("underscored.capitalization");
            int referencePosition = _source.LastIndexOf("underscored");

            Assert.IsTrue(NamingSymbolService.TryGetSymbolAtPosition(document, declarationPosition, out NamingSymbol declaration));
            Assert.AreEqual(NamingEntityKind.Style, declaration.Kind);
            Assert.IsTrue(declaration.IsDeclaration);
            Assert.IsTrue(NamingSymbolService.TryGetSymbolAtPosition(document, referencePosition, out NamingSymbol reference));
            Assert.AreEqual(NamingEntityKind.Style, reference.Kind);
            Assert.IsFalse(reference.IsDeclaration);
        }

        [TestMethod]
        public async Task FindOccurrences_ReturnsEveryDeclarationAndReference()
        {
            using EditorConfigDocument document = await CreateDocumentAsync();
            var occurrences = NamingSymbolService.FindOccurrences(document, NamingEntityKind.Style, "underscored");

            Assert.HasCount(3, occurrences);
            Assert.HasCount(2, occurrences.Where(occurrence => occurrence.IsDeclaration));
            Assert.HasCount(1, occurrences.Where(occurrence => !occurrence.IsDeclaration));
        }

        [TestMethod]
        public async Task TryGetDefinition_ReturnsFirstDeclaration()
        {
            using EditorConfigDocument document = await CreateDocumentAsync();
            int referencePosition = _source.LastIndexOf("underscored");
            NamingSymbolService.TryGetSymbolAtPosition(document, referencePosition, out NamingSymbol reference);

            bool found = NamingSymbolService.TryGetDefinition(document, reference, out NamingSymbol definition);

            Assert.IsTrue(found);
            Assert.IsTrue(definition.IsDeclaration);
            Assert.AreEqual(_source.IndexOf("underscored.capitalization"), definition.Span.Start);
        }

        [TestMethod]
        public async Task FindReferenceResults_ReturnsNavigableLocations()
        {
            using EditorConfigDocument document = await CreateDocumentAsync();

            var references = NamingSymbolService.FindReferenceResults(
                document,
                document.TextBuffer.CurrentSnapshot,
                NamingEntityKind.Style,
                "underscored");

            Assert.HasCount(3, references);
            Assert.AreEqual(1, references[0].Line);
            Assert.AreEqual("dotnet_naming_style.".Length, references[0].Column);
            StringAssert.Contains(references[0].LineText, "underscored.capitalization");
            Assert.IsTrue(references[0].Symbol.IsDeclaration);
            Assert.IsFalse(references[2].Symbol.IsDeclaration);
        }

        [TestMethod]
        public async Task TryGetRenameSpans_ReturnsAllOccurrencesInDescendingOrder()
        {
            using EditorConfigDocument document = await CreateDocumentAsync();
            int referencePosition = _source.LastIndexOf("underscored");
            NamingSymbolService.TryGetSymbolAtPosition(document, referencePosition, out NamingSymbol symbol);

            bool valid = NamingSymbolService.TryGetRenameSpans(
                document,
                symbol,
                "under_scored",
                out var spans,
                out string error);

            Assert.IsTrue(valid, error);
            Assert.HasCount(3, spans);
            Assert.IsGreaterThan(spans[1].Start, spans[0].Start);
            Assert.IsGreaterThan(spans[2].Start, spans[1].Start);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("not.valid")]
        [DataRow("has spaces")]
        public async Task TryGetRenameSpans_RejectsInvalidNames(string newName)
        {
            using EditorConfigDocument document = await CreateDocumentAsync();
            int referencePosition = _source.LastIndexOf("underscored");
            NamingSymbolService.TryGetSymbolAtPosition(document, referencePosition, out NamingSymbol symbol);

            bool valid = NamingSymbolService.TryGetRenameSpans(document, symbol, newName, out _, out string error);

            Assert.IsFalse(valid);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }

        [TestMethod]
        public async Task NamingEntityTooltip_SummarizesMembersAndReferences()
        {
            using EditorConfigDocument document = await CreateDocumentAsync();
            document.NamingEntities.TryGetEntity(NamingEntityKind.Style, "underscored", out NamingEntity entity);

            var tooltip = new NamingEntityTooltip(entity, referenceCount: 1);

            Assert.AreEqual("Naming style underscored", tooltip.Name);
            StringAssert.Contains(tooltip.Description, "capitalization = pascal_case");
            StringAssert.Contains(tooltip.Description, "required_prefix = _");
            StringAssert.Contains(tooltip.Description, "Referenced once.");
        }

        private static async Task<EditorConfigDocument> CreateDocumentAsync()
        {
            var buffer = TestTextBufferFactory.CreateTextBuffer(_source);
            EditorConfigDocument document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            await document.WaitForParsingCompleteAsync();
            return document;
        }
    }
}
