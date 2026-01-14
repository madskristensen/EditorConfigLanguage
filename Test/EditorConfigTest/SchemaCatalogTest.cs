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
            var file = new FileInfo(@"..\..\..\..\src\schema\EditorConfig.json");
            SchemaCatalog.ParseJson(file.FullName);
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
    }
}
