using System.Collections.Generic;
using System.IO;
using System.Linq;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class SchemaCatalogTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            string testDir = Path.GetDirectoryName(typeof(SchemaCatalogTest).Assembly.Location);
            string file = Path.Combine(testDir, "schema", "EditorConfig.json");
            SchemaCatalog.ParseJson(file);
        }

        [TestMethod]
        public void TryGetKeyword_StandardKeyword_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("indent_style", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("indent_style", keyword.Name);
        }

        [TestMethod]
        public void TryGetKeyword_CaseInsensitive_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("INDENT_STYLE", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
        }

        [TestMethod]
        public void TryGetKeyword_Rulers_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("rulers", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("rulers", keyword.Name);
            Assert.AreEqual(Category.Standard, keyword.Category);
            Assert.IsTrue(keyword.SupportsMultipleValues);
        }

        [TestMethod]
        public void TryGetKeyword_UnknownKeyword_ReturnsFalse()
        {
            bool result = SchemaCatalog.TryGetKeyword("unknown_keyword_xyz", out Keyword keyword);

            Assert.IsFalse(result);
            Assert.IsNull(keyword);
        }

        [TestMethod]
        public void TryGetKeyword_NullKeyword_ReturnsFalse()
        {
            bool result = SchemaCatalog.TryGetKeyword(null, out Keyword keyword);

            Assert.IsFalse(result);
            Assert.IsNull(keyword);
        }

        [TestMethod]
        public void TryGetKeyword_DotNetNamingRule_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("dotnet_naming_rule.my_rule.severity", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
        }

        [TestMethod]
        public void TryGetKeyword_DotNetDiagnostic_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("dotnet_diagnostic.CA1000.severity", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
        }

        [TestMethod]
        public void TryGetKeyword_DotNetAnalyzerDiagnostic_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("dotnet_analyzer_diagnostic.category-Design.severity", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
        }

        [TestMethod]
        public void TryGetSeverity_ValidSeverity_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetSeverity("warning", out Severity severity);

            Assert.IsTrue(result);
            Assert.IsNotNull(severity);
            Assert.AreEqual("warning", severity.Name);
        }

        [TestMethod]
        public void TryGetSeverity_InvalidSeverity_ReturnsFalse()
        {
            bool result = SchemaCatalog.TryGetSeverity("invalid_severity", out Severity severity);

            Assert.IsFalse(result);
            Assert.IsNull(severity);
        }

        [TestMethod]
        public void AllKeywords_ContainsStandardProperties()
        {
            string[] standardKeywords = ["root", "indent_style", "indent_size", "tab_width", "end_of_line", "charset", "trim_trailing_whitespace", "insert_final_newline"];

            foreach (string keywordName in standardKeywords)
            {
                bool exists = SchemaCatalog.TryGetKeyword(keywordName, out _);
                Assert.IsTrue(exists, $"Standard keyword '{keywordName}' should exist");
            }
        }

        [TestMethod]
        public void VisibleKeywords_DoesNotContainHiddenKeywords()
        {
            Assert.IsTrue(SchemaCatalog.VisibleKeywords.Any());
            Assert.IsGreaterThanOrEqualTo(SchemaCatalog.VisibleKeywords.Count(), SchemaCatalog.AllKeywords.Count());
        }

        [TestMethod]
        public void Severities_ContainsExpectedValues()
        {
            string[] expectedSeverities = ["none", "silent", "suggestion", "warning", "error"];

            foreach (string severityName in expectedSeverities)
            {
                bool exists = SchemaCatalog.TryGetSeverity(severityName, out _);
                Assert.IsTrue(exists, $"Severity '{severityName}' should exist");
            }
        }

        [TestMethod]
        public void TryGetKeyword_SpellingLanguages_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("spelling_languages", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("spelling_languages", keyword.Name);
            Assert.AreEqual(Category.VisualStudio, keyword.Category);
            Assert.IsTrue(keyword.SupportsMultipleValues);
            Assert.IsTrue(keyword.Values.Any(v => v.Name == "<language_tag>"));
        }

        [TestMethod]
        public void TryGetKeyword_StandardSpellingLanguage_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("spelling_language", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("spelling_language", keyword.Name);
            Assert.AreEqual(Category.Standard, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_SpellingCheckableTypes_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("spelling_checkable_types", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("spelling_checkable_types", keyword.Name);
            Assert.AreEqual(Category.VisualStudio, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupAddMissingErrorTagType_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_add_missing_error_tag_type", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("cpp_include_cleanup_add_missing_error_tag_type", keyword.Name);
            Assert.AreEqual(Category.CPP, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupAddMissingErrorTagType_ContainsNoneValue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_add_missing_error_tag_type", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.IsTrue(keyword.Values.Any(v => v.Name == "none"));
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupRemoveUnusedErrorTagType_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_remove_unused_error_tag_type", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("cpp_include_cleanup_remove_unused_error_tag_type", keyword.Name);
            Assert.AreEqual(Category.CPP, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupRemoveUnusedErrorTagType_ContainsDimmedValue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_remove_unused_error_tag_type", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.IsTrue(keyword.Values.Any(v => v.Name == "dimmed"));
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupExcludedFiles_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_excluded_files", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("cpp_include_cleanup_excluded_files", keyword.Name);
            Assert.AreEqual(Category.CPP, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupRequiredFiles_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_required_files", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("cpp_include_cleanup_required_files", keyword.Name);
            Assert.AreEqual(Category.CPP, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupReplacementFiles_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_replacement_files", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("cpp_include_cleanup_replacement_files", keyword.Name);
            Assert.AreEqual(Category.CPP, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupHeaderRemappings_ReturnsFalse()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_header_remappings", out Keyword keyword);

            Assert.IsFalse(result);
            Assert.IsNull(keyword);
        }

        [TestMethod]
        public void TryGetKeyword_CppIncludeCleanupAlternateFiles_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("cpp_include_cleanup_alternate_files", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("cpp_include_cleanup_alternate_files", keyword.Name);
            Assert.AreEqual(Category.CPP, keyword.Category);
        }

        [TestMethod]
        public void CurrentCppProperties_ReturnTrue()
        {
            string[] properties =
            [
                "cpp_sort_includes_error_tag_type",
                "cpp_sort_includes_priority_case_sensitive",
                "cpp_sort_includes_priority_style",
                "cpp_include_cleanup_format_after_edits",
                "cpp_include_cleanup_sort_after_edits",
                "cpp_includes_style",
                "cpp_includes_use_forward_slash"
            ];

            foreach (string property in properties)
            {
                Assert.IsTrue(SchemaCatalog.TryGetKeyword(property, out Keyword keyword), property);
                Assert.AreEqual(Category.CPP, keyword.Category, property);
            }
        }

        [TestMethod]
        public void CurrentCSharpProperties_ReturnTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("csharp_style_prefer_labeled_jump_statements", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual(Category.CSharp, keyword.Category);
            Assert.IsTrue(keyword.RequiresSeverity);
        }

        [TestMethod]
        public void DiagnosticSeverityValues_MatchDocumentation()
        {
            string[] properties =
            [
                "dotnet_diagnostic.<rule_id>.severity",
                "dotnet_analyzer_diagnostic.severity",
                "dotnet_analyzer_diagnostic.category-<category>.severity"
            ];

            foreach (string property in properties)
            {
                Assert.IsTrue(SchemaCatalog.TryGetKeyword(property, out Keyword keyword), property);
                Assert.IsTrue(keyword.Values.Any(v => v.Name == "default"), property);
                Assert.IsFalse(keyword.Values.Any(v => v.Name == "refactoring"), property);
            }
        }

        [TestMethod]
        public void CorrectedSchemaValues_MatchDocumentation()
        {
            Assert.IsTrue(SchemaCatalog.TryGetKeyword("csharp_new_line_before_open_brace", out Keyword braces));
            Assert.IsTrue(braces.Values.Any(v => v.Name == "object_collection_array_initializers"));
            Assert.IsFalse(braces.Values.Any(v => v.Name == "object_collection"));
            Assert.IsFalse(braces.Values.Any(v => v.Name == "events"));
            Assert.IsFalse(braces.Values.Any(v => v.Name == "indexers"));
            Assert.IsFalse(braces.Values.Any(v => v.Name == "local_functions"));

            Assert.IsTrue(SchemaCatalog.TryGetKeyword("visual_basic_preferred_modifier_order", out Keyword modifiers));
            Assert.AreEqual("Async", modifiers.DefaultValue.Last().Name);

            Assert.IsTrue(SchemaCatalog.TryGetKeyword("spelling_error_severity", out Keyword spellingSeverity));
            Assert.IsTrue(spellingSeverity.Values.Any(v => v.Name == "hint"));
            Assert.IsFalse(spellingSeverity.Values.Any(v => v.Name == "none"));
        }

        [TestMethod]
        public void TryGetKeyword_SpellingExclusionPath_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("spelling_exclusion_path", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("spelling_exclusion_path", keyword.Name);
            Assert.AreEqual(Category.VisualStudio, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_SpellingUseDefaultExclusionDictionary_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("spelling_use_default_exclusion_dictionary", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("spelling_use_default_exclusion_dictionary", keyword.Name);
            Assert.AreEqual(Category.VisualStudio, keyword.Category);
        }

        [TestMethod]
        public void TryGetKeyword_SpellingErrorSeverity_ReturnsTrue()
        {
            bool result = SchemaCatalog.TryGetKeyword("spelling_error_severity", out Keyword keyword);

            Assert.IsTrue(result);
            Assert.IsNotNull(keyword);
            Assert.AreEqual("spelling_error_severity", keyword.Name);
            Assert.AreEqual(Category.VisualStudio, keyword.Category);
        }

        [TestMethod]
        public void ValueConstraints_ValidateOpenAndBoundedValues()
        {
            Assert.IsTrue(SchemaCatalog.TryGetKeyword("spelling_languages", out Keyword languages));
            Assert.IsTrue(SchemaValueValidator.IsValid(languages, "en-us,da-dk"));
            Assert.IsTrue(SchemaValueValidator.IsValid(languages, "zh-Hant-TW"));
            Assert.IsTrue(SchemaValueValidator.IsValid(languages, "zh-cmn-Hans-CN"));
            Assert.IsFalse(SchemaValueValidator.IsValid(languages, "en_US"));

            Assert.IsTrue(SchemaCatalog.TryGetKeyword("indent_size", out Keyword indentSize));
            Assert.IsTrue(SchemaValueValidator.IsValid(indentSize, "tab"));
            Assert.IsTrue(SchemaValueValidator.IsValid(indentSize, "8"));
            Assert.IsFalse(SchemaValueValidator.IsValid(indentSize, "0"));

            Assert.IsTrue(SchemaCatalog.TryGetKeyword("file_header_template", out Keyword header));
            Assert.IsTrue(SchemaValueValidator.IsValid(header, "Copyright (c) Example"));

            var bounded = new Keyword(
                "bounded",
                "",
                [],
                [],
                false,
                false,
                false,
                false,
                null,
                null,
                null,
                valueKind: SchemaValueValidator.Integer,
                minimum: 1,
                maximum: 10);
            Assert.IsTrue(SchemaValueValidator.IsValid(bounded, "10"));
            Assert.IsFalse(SchemaValueValidator.IsValid(bounded, "11"));
        }

        [TestMethod]
        public void OptionalSchemaMetadata_PreservesLegacyConstructionAndSupportsAliases()
        {
            var legacy = new Keyword("legacy", "", [], [], false, false, false, false, null, null, null);
            Assert.IsTrue(legacy.MatchesName("legacy"));

            var extended = new Keyword(
                "canonical",
                "",
                ["canonical_value"],
                [],
                false,
                false,
                false,
                false,
                null,
                null,
                null,
                aliases: ["old_name"],
                valueAliases: new Dictionary<string, string>
                {
                    ["old_value"] = "canonical_value"
                },
                deprecated: true,
                replacement: "preferred");

            Assert.IsTrue(extended.MatchesName("old_name"));
            Assert.IsTrue(SchemaValueValidator.IsValid(extended, "old_value"));
            Assert.IsTrue(extended.IsDeprecated);
            Assert.AreEqual("preferred", extended.Replacement);
        }

        [TestMethod]
        public void CompoundKeys_UseGenericPlaceholderMatching()
        {
            Assert.IsTrue(SchemaCatalog.TryGetKeyword("dotnet_code_quality.CA1000.some_option", out Keyword compound));
            Assert.AreEqual("dotnet_code_quality.<rule_id>.<option>", compound.Name);

            Assert.IsTrue(SchemaCatalog.TryGetKeyword("dotnet_naming_symbols.private_fields.applicable_kinds", out Keyword declaration));
            Assert.AreEqual("naming_symbols", declaration.DeclarationKind);
            Assert.IsTrue(declaration.TryGetPlaceholderValue("dotnet_naming_symbols.private_fields.applicable_kinds", out string declarationName));
            Assert.AreEqual("private_fields", declarationName);

            Assert.IsTrue(SchemaCatalog.TryGetKeyword("dotnet_naming_rule.private_fields.symbols", out Keyword reference));
            Assert.AreEqual("naming_symbols", reference.ReferenceKind);
        }
    }
}
