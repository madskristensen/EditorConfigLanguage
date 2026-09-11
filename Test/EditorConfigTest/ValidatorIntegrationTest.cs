using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    [DoNotParallelize]
    public class ValidatorIntegrationTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            string testDir = Path.GetDirectoryName(typeof(ValidatorIntegrationTest).Assembly.Location);
            SchemaCatalog.ParseJson(Path.Combine(testDir, "schema", "EditorConfig.json"));
        }

        [TestMethod]
        public async Task ValidateDocument_ReportsPropertyAndSeverityProblems()
        {
            const string source = """
                [*.cs]
                indent_style = invalid
                indent_style = space
                unknown_property = true
                csharp_style_var_for_built_in_types = true:invalid
                """;

            IReadOnlyList<string> codes = await ValidateAsync(source);

            CollectionAssert.IsSubsetOf(
                new[] { "EC101", "EC112", "EC114" },
                codes.ToArray());
        }

        [TestMethod]
        public async Task ValidateDocument_ReportsUndeclaredNamingReferences()
        {
            const string source = """
                [*.cs]
                dotnet_naming_rule.private_fields.symbols = missing_symbols
                dotnet_naming_rule.private_fields.style = missing_style
                dotnet_naming_rule.private_fields.severity = suggestion
                """;

            IReadOnlyList<string> codes = await ValidateAsync(source);

            CollectionAssert.Contains(codes.ToArray(), "EC118");
            CollectionAssert.Contains(codes.ToArray(), "EC122");
        }

        [TestMethod]
        public async Task ValidateDocument_HonorsErrorSuppressions()
        {
            const string source = """
                # Suppress: EC112
                [*.cs]
                unknown_property = true
                """;

            IReadOnlyList<string> codes = await ValidateAsync(source);

            CollectionAssert.DoesNotContain(codes.ToArray(), "EC112");
        }

        [TestMethod]
        public async Task ValidateDocument_AcceptsCompleteNamingConfiguration()
        {
            const string source = """
                [*.cs]
                dotnet_naming_symbols.private_fields.applicable_kinds = field
                dotnet_naming_symbols.private_fields.applicable_accessibilities = private
                dotnet_naming_style.underscored.capitalization = camel_case
                dotnet_naming_style.underscored.required_prefix = _
                dotnet_naming_rule.private_fields.symbols = private_fields
                dotnet_naming_rule.private_fields.style = underscored
                dotnet_naming_rule.private_fields.severity = suggestion
                """;

            IReadOnlyList<string> codes = await ValidateAsync(source);

            CollectionAssert.DoesNotContain(codes.ToArray(), "EC118");
            CollectionAssert.DoesNotContain(codes.ToArray(), "EC122");
            CollectionAssert.DoesNotContain(codes.ToArray(), "EC123");
        }

        private static async Task<IReadOnlyList<string>> ValidateAsync(string source)
        {
            var buffer = TestTextBufferFactory.CreateTextBuffer(source);
            using EditorConfigDocument document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            await document.WaitForParsingCompleteAsync();
            using EditorConfigValidator validator = EditorConfigValidator.FromDocument(document);
            await validator.RequestValidationAsync(force: true);

            return document.ParseItems
                .SelectMany(item => item.Errors)
                .Select(error => error.Name)
                .ToArray();
        }
    }
}
