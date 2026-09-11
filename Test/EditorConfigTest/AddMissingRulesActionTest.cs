using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class AddMissingRulesActionTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            string testDir = Path.GetDirectoryName(typeof(AddMissingRulesActionTest).Assembly.Location);
            string file = Path.Combine(testDir, "schema", "EditorConfig.json");
            SchemaCatalog.ParseJson(file);
        }

        [TestMethod]
        public void FindMissingRulesAll_ReturnsEveryFiniteSupportedSectionProperty()
        {
            string[] expected = SchemaCatalog.VisibleKeywords
                .Where(keyword =>
                    keyword.IsSupported &&
                    keyword.Name.IndexOf('<') < 0 &&
                    !keyword.Name.Equals(SchemaCatalog.Root, StringComparison.OrdinalIgnoreCase))
                .Select(keyword => keyword.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string[] actual = AddMissingRulesAction.FindMissingRulesAll([]).Select(keyword => keyword.Name).ToArray();

            CollectionAssert.AreEquivalent(expected, actual);
        }

        [TestMethod]
        public void FindMissingRulesAll_ExcludesExistingPropertiesCaseInsensitively()
        {
            List<Keyword> missing = AddMissingRulesAction.FindMissingRulesAll(["INDENT_STYLE"]);

            Assert.IsFalse(missing.Any(keyword => keyword.Name.Equals("indent_style", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void FindMissingRulesAll_DoesNotEmitPlaceholderOrUnsupportedProperties()
        {
            List<Keyword> missing = AddMissingRulesAction.FindMissingRulesAll([]);

            Assert.IsFalse(missing.Any(keyword => keyword.Name.IndexOf('<') >= 0));
            Assert.IsFalse(missing.Any(keyword => !keyword.IsSupported));
            Assert.IsFalse(missing.Any(keyword => keyword.Name.Equals(SchemaCatalog.Root, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
