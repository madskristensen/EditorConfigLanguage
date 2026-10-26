using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Adornments;
using Moq;

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
        public async Task ValidationNotificationExposesReadableSquiggles()
        {
            var buffer = TestTextBufferFactory.CreateTextBuffer("root=true\r\n[*]\r\nunknown_property=true");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            using EditorConfigValidator validator = EditorConfigValidator.FromDocument(document);
            var view = new Mock<IWpfTextView>();
            view.SetupGet(value => value.TextBuffer).Returns(buffer);
            using var tagger = new ErrorTagger(view.Object);
            bool busy = true;
            bool snapshotReady = false;
            ITagSpan<IErrorTag>[] tags = null;
            validator.Validated += (sender, args) =>
            {
                busy = validator.IsValidating;
                snapshotReady = validator.HasValidatedSnapshot(buffer.CurrentSnapshot);
                tags = tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length))).ToArray();
            };

            await validator.RequestValidationAsync(force: true);

            Assert.IsFalse(busy);
            Assert.IsTrue(snapshotReady);
            Assert.IsNotNull(tags);
            Assert.HasCount(1, tags);
            Assert.AreEqual("unknown_property", tags[0].Span.GetText());
            Assert.AreEqual("EC112", tags[0].Tag.ToolTipContent);
        }

        [TestMethod]
        public void GlobbingChecksFullDirectoryNamesAndIncompleteSearches()
        {
            string folder = Path.Combine(Path.GetTempPath(), "EditorConfigGlobbingTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string binary = Directory.CreateDirectory(Path.Combine(folder, "binary")).FullName;
                File.WriteAllText(Path.Combine(binary, "match.cs"), "");
                string excluded = Directory.CreateDirectory(Path.Combine(folder, "bin")).FullName;
                File.WriteAllText(Path.Combine(excluded, "excluded.vb"), "");
                Assert.AreEqual(EditorConfigValidator.MatchResult.Match, EditorConfigValidator.DoesFilesMatch(folder, "[*.cs]"));
                Assert.AreEqual(EditorConfigValidator.MatchResult.NoMatch, EditorConfigValidator.DoesFilesMatch(folder, "[*.vb]"));
                Assert.AreEqual(EditorConfigValidator.MatchResult.NoMatch, EditorConfigValidator.DoesFilesMatch(folder, "[*.missing]"));

                string deep = folder;
                for (int i = 0; i < 6; i++)
                    deep = Directory.CreateDirectory(Path.Combine(deep, "nested")).FullName;
                File.WriteAllText(Path.Combine(deep, "deep.txt"), "");
                Assert.AreEqual(EditorConfigValidator.MatchResult.Incomplete, EditorConfigValidator.DoesFilesMatch(folder, "[*.txt]"));
                Assert.AreEqual(EditorConfigValidator.MatchResult.Incomplete, EditorConfigValidator.DoesFilesMatch(folder, "[*.missing]"));
                Assert.AreEqual(EditorConfigValidator.MatchResult.Match, EditorConfigValidator.DoesFilesMatch(folder, "[*.cs]"));
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
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
        public async Task ValidateDocument_AcceptsTrailingCommentAfterValue()
        {
            const string source = """
                [*.cs]
                dotnet_style_prefer_foreach_explicit_cast_in_source = when_strongly_typed # IDE0220
                """;

            IReadOnlyList<string> codes = await ValidateAsync(source);

            CollectionAssert.DoesNotContain(codes.ToArray(), "EC114");
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
