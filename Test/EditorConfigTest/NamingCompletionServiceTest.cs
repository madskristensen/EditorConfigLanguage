using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class NamingCompletionServiceTest
    {
        private NamingEntityIndex _index;

        [TestInitialize]
        public async Task Initialize()
        {
            string testDir = Path.GetDirectoryName(typeof(NamingCompletionServiceTest).Assembly.Location);
            SchemaCatalog.ParseJson(Path.Combine(testDir, "schema", "EditorConfig.json"));

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
            _index = document.Sections[0].NamingEntities;
        }

        [TestMethod]
        public void EntityNameCompletion_IncludesDeclaredAndReferencedNames()
        {
            const string line = "dotnet_naming_symbols.";

            NamingCompletionContext context = NamingCompletionService.GetContext(line, line.Length, [_index]);

            Assert.AreEqual(NamingCompletionKind.EntityName, context.Kind);
            Assert.AreEqual(line.Length, context.SpanStart);
            CollectionAssert.AreEquivalent(
                new[] { "private_fields", "public_symbols" },
                context.Items.Select(item => item.Name).ToArray());
        }

        [TestMethod]
        public void MemberCompletion_ReturnsUnusedMembers()
        {
            const string line = "dotnet_naming_symbols.private_fields.";

            NamingCompletionContext context = NamingCompletionService.GetContext(line, line.Length, [_index]);

            Assert.AreEqual(NamingCompletionKind.Member, context.Kind);
            CollectionAssert.AreEquivalent(
                new[] { "applicable_accessibilities", "required_modifiers" },
                context.Items.Select(item => item.Name).ToArray());
        }

        [TestMethod]
        public void ReferenceValueCompletion_ReturnsDeclaredNamesOnly()
        {
            const string line = "dotnet_naming_rule.new_rule.symbols = ";

            NamingCompletionContext context = NamingCompletionService.GetContext(line, line.Length, [_index]);

            Assert.AreEqual(NamingCompletionKind.ReferenceValue, context.Kind);
            CollectionAssert.AreEquivalent(
                new[] { "private_fields" },
                context.Items.Select(item => item.Name).ToArray());
        }

        [TestMethod]
        public void NonNamingProperty_DoesNotParticipate()
        {
            const string line = "indent_style = ";

            NamingCompletionContext context = NamingCompletionService.GetContext(line, line.Length, [_index]);

            Assert.AreEqual(NamingCompletionKind.None, context.Kind);
            Assert.IsEmpty(context.Items);
        }
    }
}
