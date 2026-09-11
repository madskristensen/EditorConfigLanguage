using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class NamingEntityIndexTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            string testDir = Path.GetDirectoryName(typeof(NamingEntityIndexTest).Assembly.Location);
            string file = Path.Combine(testDir, "schema", "EditorConfig.json");
            SchemaCatalog.ParseJson(file);
        }

        [TestMethod]
        public async Task Create_IndexesPartialDeclarationsAndReferences()
        {
            const string source = """
                [*.cs]
                dotnet_naming_symbols.private_fields.applicable_kinds = field
                dotnet_naming_style.underscored.required_prefix = _
                dotnet_naming_rule.private_fields_underscored.symbols = private_fields
                dotnet_naming_rule.private_fields_underscored.style = underscored
                dotnet_naming_rule.public_members.symbols = public_symbols
                """;
            var buffer = TestTextBufferFactory.CreateTextBuffer(source);
            using EditorConfigDocument document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            await document.WaitForParsingCompleteAsync();

            Assert.IsTrue(document.NamingEntities.TryGetEntity(NamingEntityKind.Symbols, "PRIVATE_FIELDS", out NamingEntity symbols));
            Assert.IsTrue(symbols.Members.ContainsKey("applicable_kinds"));
            Assert.IsTrue(document.NamingEntities.TryGetEntity(NamingEntityKind.Style, "underscored", out NamingEntity style));
            Assert.IsTrue(style.Members.ContainsKey("required_prefix"));
            Assert.IsTrue(document.NamingEntities.TryGetEntity(NamingEntityKind.Rule, "private_fields_underscored", out _));
            CollectionAssert.AreEquivalent(
                new[] { "private_fields", "public_symbols" },
                document.NamingEntities.GetNames(NamingEntityKind.Symbols, includeReferencedNames: true).ToArray());
            CollectionAssert.AreEquivalent(
                new[] { "private_fields" },
                document.NamingEntities.GetNames(NamingEntityKind.Symbols).ToArray());
            Assert.HasCount(1, document.NamingEntities.GetReferences(NamingEntityKind.Symbols, "private_fields"));
        }
    }
}
