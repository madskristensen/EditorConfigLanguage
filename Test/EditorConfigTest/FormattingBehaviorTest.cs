using System;
using System.Linq;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;

namespace EditorConfigTest
{
    [TestClass]
    public class FormattingBehaviorTest
    {
        [TestMethod]
        public void FormatPropertyText_AppliesAlignmentAndSeveritySpacing()
        {
            Property property = CreateProperty("csharp_style_var_elsewhere", "true", "warning");

            string result = EditorConfigFormatter.FormatPropertyText(property, 32, 2, 1, 0, 2);

            Assert.AreEqual("csharp_style_var_elsewhere        = true:  warning", result);
        }

        [TestMethod]
        public void FormatPropertyText_LeavesIncompletePropertyWithoutSeparators()
        {
            Property property = CreateProperty("indent_style", null, null);

            string result = EditorConfigFormatter.FormatPropertyText(property, 12, 1, 1, 1, 1);

            Assert.AreEqual("indent_style", result);
        }

        [TestMethod]
        public void SortSectionText_SortsPropertiesCaseInsensitivelyWithinGroups()
        {
            string result = SortPropertiesAction.SortSectionText("[*.cs]", new[]
            {
                "dotnet_style_b = true",
                "indent_style = space",
                "csharp_style_z = true",
                "dotnet_style_a = true",
            });

            Assert.AreEqual(
                Join("[*.cs]", "indent_style = space", "csharp_style_z = true", "dotnet_style_a = true", "dotnet_style_b = true"),
                result);
        }

        [TestMethod]
        public void SortSectionText_KeepsCommentedGroupsAndBlankLineBlocksTogether()
        {
            string result = SortPropertiesAction.SortSectionText("[*.cs]", new[]
            {
                "# Formatting",
                "indent_size = 4",
                "indent_style = space",
                "",
                "; Naming",
                "dotnet_naming_rule.z.severity = warning",
                "dotnet_naming_rule.a.severity = error",
            });

            Assert.AreEqual(
                Join(
                    "[*.cs]",
                    "# Formatting",
                    "indent_size = 4",
                    "indent_style = space",
                    "",
                    "; Naming",
                    "dotnet_naming_rule.a.severity = error",
                    "dotnet_naming_rule.z.severity = warning"),
                result);
        }

        [TestMethod]
        public void SortSectionText_PreservesAdjacentCommentHeaders()
        {
            string result = SortPropertiesAction.SortSectionText("[*]", new[]
            {
                "# First line",
                "# Second line",
                "trim_trailing_whitespace = true",
            });

            Assert.AreEqual(
                Join("[*]", "# First line", "# Second line", "trim_trailing_whitespace = true"),
                result);
        }

        private static string Join(params string[] lines) => string.Join(Environment.NewLine, lines);

        private static Property CreateProperty(string name, string value, string severity)
        {
            var keyword = new ParseItem(null, ItemType.Keyword, new Span(0, name.Length), name);
            var property = new Property(keyword);
            int position = name.Length + 3;

            if (value != null)
            {
                property.Value = new ParseItem(null, ItemType.Value, new Span(position, value.Length), value);
                position += value.Length + 3;
            }

            if (severity != null)
                property.Severity = new ParseItem(null, ItemType.Severity, new Span(position, severity.Length), severity);

            return property;
        }
    }
}
