using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace EditorConfigTest
{
    [TestClass]
    public class ValidationTest
    {
        private const string DefaultPrefixes = "resharper_, idea_, roslynator_, ij_";

        [TestMethod]
        public void HasIgnoredPrefix_WithDefaultPrefixes_MatchesResharper()
        {
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("resharper_csharp_braces_for_ifelse", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithDefaultPrefixes_MatchesIdea()
        {
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("idea_some_setting", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithDefaultPrefixes_MatchesRoslynator()
        {
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("roslynator_analyzers.enabled_by_default", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithDefaultPrefixes_MatchesIj()
        {
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("ij_any_setting", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithDefaultPrefixes_DoesNotMatchKnownKeywords()
        {
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("indent_size", DefaultPrefixes));
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("indent_style", DefaultPrefixes));
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("dotnet_style_qualification_for_field", DefaultPrefixes));
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("csharp_style_var_for_built_in_types", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_IsCaseInsensitive()
        {
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("RESHARPER_some_setting", DefaultPrefixes));
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("Resharper_Some_Setting", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithEmptyPrefixes_ReturnsFalse()
        {
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("resharper_some_setting", ""));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithNullPrefixes_ReturnsFalse()
        {
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("resharper_some_setting", null));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithNullKeyword_ReturnsFalse()
        {
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix(null, DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithEmptyKeyword_ReturnsFalse()
        {
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("", DefaultPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_WithCustomPrefixes_MatchesCustom()
        {
            string customPrefixes = "custom_, myprefix_";

            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("custom_setting", customPrefixes));
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("myprefix_option", customPrefixes));
            Assert.IsFalse(ValidationOptions.HasIgnoredPrefix("resharper_setting", customPrefixes));
        }

        [TestMethod]
        public void HasIgnoredPrefix_HandlesWhitespaceInPrefixList()
        {
            string prefixesWithWhitespace = "  prefix1_  ,  prefix2_  ";

            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("prefix1_setting", prefixesWithWhitespace));
            Assert.IsTrue(ValidationOptions.HasIgnoredPrefix("prefix2_setting", prefixesWithWhitespace));
        }

        [TestMethod]
        public void GlobalConfig_DoesNotReportOnlyRootAllowedForTopLevelProperties()
        {
            bool result = EditorConfigValidator.ShouldReportOnlyRootAllowed(isGlobalConfig: true, isRootProperty: false);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void EditorConfig_StillReportsOnlyRootAllowedForNonRootTopLevelProperties()
        {
            bool result = EditorConfigValidator.ShouldReportOnlyRootAllowed(isGlobalConfig: false, isRootProperty: false);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GlobalConfig_MetadataProperties_AreRecognized()
        {
            Assert.IsTrue(EditorConfigValidator.IsGlobalConfigMetadataProperty(Constants.GlobalConfigIsGlobalPropertyName));
            Assert.IsTrue(EditorConfigValidator.IsGlobalConfigMetadataProperty(Constants.GlobalConfigLevelPropertyName));
            Assert.IsFalse(EditorConfigValidator.IsGlobalConfigMetadataProperty(SchemaCatalog.Root));
        }

        [TestMethod]
        public void GlobalConfig_MetadataValues_AreValidated()
        {
            Assert.IsTrue(EditorConfigValidator.IsValidGlobalConfigMetadataValue(Constants.GlobalConfigIsGlobalPropertyName, "true"));
            Assert.IsFalse(EditorConfigValidator.IsValidGlobalConfigMetadataValue(Constants.GlobalConfigIsGlobalPropertyName, "false"));
            Assert.IsTrue(EditorConfigValidator.IsValidGlobalConfigMetadataValue(Constants.GlobalConfigLevelPropertyName, "100"));
            Assert.IsFalse(EditorConfigValidator.IsValidGlobalConfigMetadataValue(Constants.GlobalConfigLevelPropertyName, "high"));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GlobalConfig_ArbitraryFileWithMarker_DoesNotReportOnlyRootAllowed()
        {
            var buffer = TestTextBufferFactory.CreateTextBuffer("is_global = true\r\ndotnet_diagnostic.CA1000.severity = warning");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\analyzers.props");
            await document.WaitForParsingCompleteAsync();

            Assert.IsTrue(document.IsGlobalConfig);
            Assert.IsFalse(EditorConfigValidator.ShouldReportOnlyRootAllowed(document.IsGlobalConfig, isRootProperty: false));
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GlobalConfig_ProjectNamedFile_IsImplicitlyGlobal()
        {
            var buffer = TestTextBufferFactory.CreateTextBuffer("dotnet_diagnostic.CA1000.severity = warning");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\ProjectName.globalconfig");
            await document.WaitForParsingCompleteAsync();

            Assert.IsTrue(document.IsGlobalConfig);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task GlobalConfig_MarkerMustBeRootLevelTrueWithoutSeverity()
        {
            var buffer = TestTextBufferFactory.CreateTextBuffer("[*.cs]\r\nis_global = true");
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\analyzers.props");
            await document.WaitForParsingCompleteAsync();

            Assert.IsFalse(document.IsGlobalConfig);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task NamingDeclarations_ReportMissingRequiredMembers()
        {
            const string source = """
                [*.cs]
                dotnet_naming_symbols.private_fields.applicable_kinds = field
                dotnet_naming_style.underscored.required_prefix = _
                dotnet_naming_rule.private_fields_underscored.symbols = private_fields
                """;
            var buffer = TestTextBufferFactory.CreateTextBuffer(source);
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            await document.WaitForParsingCompleteAsync();
            NamingEntityIndex index = document.Sections[0].NamingEntities;

            index.TryGetEntity(NamingEntityKind.Symbols, "private_fields", out NamingEntity symbols);
            index.TryGetEntity(NamingEntityKind.Style, "underscored", out NamingEntity style);
            index.TryGetEntity(NamingEntityKind.Rule, "private_fields_underscored", out NamingEntity rule);

            CollectionAssert.AreEquivalent(
                new[] { "applicable_accessibilities" },
                EditorConfigValidator.GetMissingNamingMembers(symbols).ToArray());
            CollectionAssert.AreEquivalent(
                new[] { "capitalization" },
                EditorConfigValidator.GetMissingNamingMembers(style).ToArray());
            CollectionAssert.AreEquivalent(
                new[] { "style", "severity" },
                EditorConfigValidator.GetMissingNamingMembers(rule).ToArray());
        }
    }
}
