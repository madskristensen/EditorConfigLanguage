using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EditorConfigTest
{
    [TestClass]
    public class PropertyRegexTest
    {
        // Mirror the regex from EditorConfigDocumentParser
        private static readonly Regex _property = new(@"^\s*(?<keyword>[^;\[#:\s=]+)\s*[=:]?\s*(?<value>[^;#]*?)(?:\s*:\s*(?<severity>none|silent|suggestion|warning|error|default|refactoring))?\s*(?=[;#]|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [TestMethod]
        public void SimpleProperty()
        {
            Match match = _property.Match("indent_style = space");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("indent_style", match.Groups["keyword"].Value);
            Assert.AreEqual("space", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void PropertyWithSeverity()
        {
            Match match = _property.Match("dotnet_style_qualification_for_field = false:suggestion");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("dotnet_style_qualification_for_field", match.Groups["keyword"].Value);
            Assert.AreEqual("false", match.Groups["value"].Value.Trim());
            Assert.AreEqual("suggestion", match.Groups["severity"].Value);
        }

        [TestMethod]
        public void PropertyWithSeverityUpperCase()
        {
            Match match = _property.Match("dotnet_style_x = true:WARNING");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("true", match.Groups["value"].Value.Trim());
            Assert.AreEqual("WARNING", match.Groups["severity"].Value);
        }

        [TestMethod]
        public void ValueWithColonFilePath()
        {
            Match match = _property.Match(@"generated_code = C:\Users\test\file.cs");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("generated_code", match.Groups["keyword"].Value);
            Assert.AreEqual(@"C:\Users\test\file.cs", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void ValueWithMultipleColons()
        {
            Match match = _property.Match(@"some_path = D:\Projects\My:Special:Folder\file.txt");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("some_path", match.Groups["keyword"].Value);
            Assert.AreEqual(@"D:\Projects\My:Special:Folder\file.txt", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void ValueWithColonButNotValidSeverity()
        {
            Match match = _property.Match("some_rule = value:notaseverity");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("some_rule", match.Groups["keyword"].Value);
            Assert.AreEqual("value:notaseverity", match.Groups["value"].Value.Trim());
            Assert.IsFalse(match.Groups["severity"].Success);
        }

        [TestMethod]
        public void FilePathWithSeverity()
        {
            Match match = _property.Match(@"some_rule = C:\path\file.cs:warning");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("some_rule", match.Groups["keyword"].Value);
            Assert.AreEqual(@"C:\path\file.cs", match.Groups["value"].Value.Trim());
            Assert.AreEqual("warning", match.Groups["severity"].Value);
        }

        [TestMethod]
        public void PropertyWithTrailingComment()
        {
            Match match = _property.Match("indent_size = 4 # comment");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("indent_size", match.Groups["keyword"].Value);
            Assert.AreEqual("4", match.Groups["value"].Value.Trim());
        }

        [TestMethod]
        public void AllValidSeverities()
        {
            string[] severities = { "none", "silent", "suggestion", "warning", "error", "default", "refactoring" };

            foreach (string severity in severities)
            {
                Match match = _property.Match($"rule = value:{severity}");

                Assert.IsTrue(match.Success, $"Failed for severity: {severity}");
                Assert.AreEqual(severity, match.Groups["severity"].Value, $"Severity mismatch for: {severity}");
            }
        }

        [TestMethod]
        public void RootProperty()
        {
            Match match = _property.Match("root = true");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("root", match.Groups["keyword"].Value);
            Assert.AreEqual("true", match.Groups["value"].Value.Trim());
        }

        [TestMethod]
        public void PropertyWithLeadingWhitespace()
        {
            Match match = _property.Match("    indent_style = tabs");

            Assert.IsTrue(match.Success);
            Assert.AreEqual("indent_style", match.Groups["keyword"].Value);
            Assert.AreEqual("tabs", match.Groups["value"].Value.Trim());
        }
    }
}
