using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
