using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;

namespace EditorConfigTest
{
    [TestClass]
    public class FormattingBehaviorTest
    {
        [TestMethod]
        public async Task Format_UsesOneSnapshotAndPreservesComments()
        {
            const string source = "  root=true   # root\r\n  [*]  \r\n    indent_style=space   # keep\r\n  indent_size  =  4 \r\n";
            var buffer = TestTextBufferFactory.CreateTextBuffer(source);
            using var document = EditorConfigDocument.CreateForTest(buffer, @"C:\repo\.editorconfig");
            buffer.Properties.AddProperty(typeof(EditorConfigDocument), document);
            await document.WaitForParsingCompleteAsync();
            var replacements = new List<(int Start, int Length, string Text)>();
            var edit = new Mock<ITextEdit>();
            edit.SetupGet(value => value.Snapshot).Returns(buffer.CurrentSnapshot);
            edit.SetupGet(value => value.HasEffectiveChanges).Returns(true);
            edit.Setup(value => value.Replace(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback((int start, int length, string text) => replacements.Add((start, length, text)))
                .Returns(true);
            edit.Setup(value => value.Apply()).Returns(() =>
            {
                string text = source;
                foreach (var replacement in replacements.OrderByDescending(value => value.Start))
                    text = text.Remove(replacement.Start, replacement.Length).Insert(replacement.Start, replacement.Text);
                return TestTextBufferFactory.SetText(buffer, text);
            });
            Mock.Get(buffer).Setup(value => value.CreateEdit()).Returns(edit.Object);

            bool changed = new EditorConfigFormatter(buffer).Format(FormattingType.None, 1, 1, 0, 0);

            Assert.IsTrue(changed);
            Assert.AreEqual("root = true   # root\r\n[*]\r\nindent_style = space   # keep\r\nindent_size = 4\r\n", buffer.CurrentSnapshot.GetText());
            Mock.Get(buffer).Verify(value => value.CreateEdit(), Times.Once);
            edit.Verify(value => value.Apply(), Times.Once);
        }

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
