using EditorConfig;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class PropertyRegexTest
    {
        [TestMethod]
        public void SimpleProperty()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("indent_style = space", out var match));

            Assert.AreEqual("indent_style", match.Groups["keyword"].Value);
            Assert.AreEqual("space", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void PropertyWithSeverity()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("dotnet_style_qualification_for_field = false:suggestion", out var match));

            Assert.AreEqual("dotnet_style_qualification_for_field", match.Groups["keyword"].Value);
            Assert.AreEqual("false", match.Groups["value"].Value.Trim());
            Assert.AreEqual("suggestion", match.Groups["severity"].Value);
        }

        [TestMethod]
        public void PropertyWithSeverityUpperCase()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("dotnet_style_x = true:WARNING", out var match));

            Assert.AreEqual("true", match.Groups["value"].Value.Trim());
            Assert.AreEqual("WARNING", match.Groups["severity"].Value);
        }

        [TestMethod]
        public void ValueWithColonFilePath()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty(@"generated_code = C:\Users\test\file.cs", out var match));

            Assert.AreEqual("generated_code", match.Groups["keyword"].Value);
            Assert.AreEqual(@"C:\Users\test\file.cs", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void ValueWithMultipleColons()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty(@"some_path = D:\Projects\My:Special:Folder\file.txt", out var match));

            Assert.AreEqual("some_path", match.Groups["keyword"].Value);
            Assert.AreEqual(@"D:\Projects\My:Special:Folder\file.txt", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void ValueWithColonButNotValidSeverity()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("some_rule = value:notaseverity", out var match));

            Assert.AreEqual("some_rule", match.Groups["keyword"].Value);
            Assert.AreEqual("value:notaseverity", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void FilePathWithSeverity()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty(@"some_rule = C:\path\file.cs:warning", out var match));

            Assert.AreEqual("some_rule", match.Groups["keyword"].Value);
            Assert.AreEqual(@"C:\path\file.cs", match.Groups["value"].Value.Trim());
            Assert.AreEqual("warning", match.Groups["severity"].Value);
        }

        [TestMethod]
        public void PropertyWithTrailingHashComment()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("indent_size = 4 # comment", out var match));

            Assert.AreEqual("indent_size", match.Groups["keyword"].Value);
            Assert.AreEqual("4", match.Groups["value"].Value.Trim());
            Assert.AreEqual("# comment", match.Groups["comment"].Value);
        }

        [TestMethod]
        public void AllValidSeverities()
        {
            string[] severities = { "none", "silent", "suggestion", "warning", "error", "default", "refactoring" };

            foreach (string severity in severities)
            {
                Assert.IsTrue(EditorConfigDocument.TryMatchProperty($"rule = value:{severity}", out var match), $"Failed for severity: {severity}");

                Assert.AreEqual(severity, match.Groups["severity"].Value, $"Severity mismatch for: {severity}");
            }
        }

        [TestMethod]
        public void RootProperty()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("root = true", out var match));

            Assert.AreEqual("root", match.Groups["keyword"].Value);
            Assert.AreEqual("true", match.Groups["value"].Value.Trim());
        }

        [TestMethod]
        public void PropertyWithLeadingWhitespace()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("    indent_style = tabs", out var match));

            Assert.AreEqual("indent_style", match.Groups["keyword"].Value);
            Assert.AreEqual("tabs", match.Groups["value"].Value.Trim());
        }

        [TestMethod]
        public void ColonIsNotAPropertyDelimiter()
        {
            Assert.IsFalse(EditorConfigDocument.TryMatchProperty("indent_style: space", out _));
        }

        [TestMethod]
        public void MissingDelimiterIsNotAProperty()
        {
            Assert.IsFalse(EditorConfigDocument.TryMatchProperty("indent_style space", out _));
        }

        [TestMethod]
        public void PropertyWithTrailingSemicolonComment()
        {
            Assert.IsTrue(EditorConfigDocument.TryMatchProperty("custom_value = first;second", out var match));
            Assert.AreEqual("first", match.Groups["value"].Value);
            Assert.AreEqual(";second", match.Groups["comment"].Value);
        }
    }
}
