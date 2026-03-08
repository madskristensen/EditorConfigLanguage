using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;

namespace EditorConfigTest
{
    [TestClass]
    public class SectionNameMatcherTest
    {
        [TestMethod]
        public void SimpleWildcard_MatchesCsFiles()
        {
            // Pattern without brackets - matches any .cs file
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("*.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/file.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/deep/nested/path/file.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file.vb"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file.csx"));
        }

        [TestMethod]
        public void SimpleWildcard_MatchesMultipleExtensions()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("*.{cs,vb}");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/file.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file.vb"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file.fs"));
        }

        [TestMethod]
        public void DoubleAsterisk_MatchesAnyPath()
        {
            // ** matches any path including directory separators
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("**/*.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/a/b/c/d/file.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file.cs"));
        }

        [TestMethod]
        public void QuestionMark_MatchesSingleCharacter()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file?.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/file1.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/fileA.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file10.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file.cs"));
        }

        [TestMethod]
        public void NumberRange_MatchesInRange()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file{1..5}.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/file1.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file3.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file5.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file0.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file6.cs"));
        }

        [TestMethod]
        public void CharacterClass_MatchesSpecifiedChars()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file[abc].cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/filea.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/fileb.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/filec.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/filed.cs"));
        }

        [TestMethod]
        public void NegatedCharacterClass_ExcludesSpecifiedChars()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file[!abc].cs");

            Assert.IsNotNull(matcher);
            Assert.IsFalse(matcher.Value.IsMatch("/project/filea.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/fileb.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/filed.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file1.cs"));
        }

        [TestMethod]
        public void AbsolutePath_MatchesFromRoot()
        {
            // Pattern starting with / matches from root
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("/src/*.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/src/file.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/other/src/file.cs"));
        }

        [TestMethod]
        public void RelativePathWithDirectory_MatchesSubdirectory()
        {
            // Pattern with / but not starting with / prepends just /
            // So src/*.cs matches /src/*.cs paths
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("src/*.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/src/file.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/other/src/file.cs"));
        }

        [TestMethod]
        public void AllFiles_MatchesEverything()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("*");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/any/path/file.txt"));
            Assert.IsTrue(matcher.Value.IsMatch("/file"));
        }

        [TestMethod]
        public void SpecificFileName_MatchesExactly()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("Makefile");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/Makefile"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/Makefile.txt"));
        }

        [TestMethod]
        public void ComplexPattern_CSharpAndVBInSpecificFolders()
        {
            // Pattern src/**/*.{cs,vb} requires at least one directory after src/
            // because the / between ** and * is literal
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("src/**/*.{cs,vb}");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/src/subdir/file.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/src/nested/deep/file.vb"));
            // Direct files in src/ don't match because of the / after **
            Assert.IsFalse(matcher.Value.IsMatch("/src/file.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/other/file.cs"));
        }

        [TestMethod]
        public void DoubleAsteriskOnly_MatchesAnyFile()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("**");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/any/deep/path/file.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/file.txt"));
        }

        [TestMethod]
        public void ExtensionOnly_MatchesFilesWithExtension()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("*.xml");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/config/app.xml"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/web.xml"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file.json"));
        }

        [TestMethod]
        public void EscapedBraces_MatchLiteralBraces()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher(@"file\{name\}.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/file{name}.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/fileA.cs"));
        }

        [TestMethod]
        public void DescendingNumberRange_MatchesNormalizedRange()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file{5..1}.cs");

            Assert.IsNotNull(matcher);
            Assert.IsTrue(matcher.Value.IsMatch("/project/file1.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file3.cs"));
            Assert.IsTrue(matcher.Value.IsMatch("/project/file5.cs"));
            Assert.IsFalse(matcher.Value.IsMatch("/project/file6.cs"));
        }

        [TestMethod]
        public void UnclosedCharacterClass_ReturnsNullMatcher()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file[abc.cs");

            Assert.IsNull(matcher);
        }

        [TestMethod]
        public void TrailingEscape_ReturnsNullMatcher()
        {
            AnalyzerConfig.SectionNameMatcher? matcher = AnalyzerConfig.TryCreateSectionNameMatcher("file\\");

            Assert.IsNull(matcher);
        }
    }

    [TestClass]
    public class ValidatorGlobbingTest
    {
        [TestMethod]
        public void DoesFilesMatch_FindsFileAtMaxDepth()
        {
            string root = CreateTempRoot();

            try
            {
                string deepestAllowed = CreateNestedDirectory(root, 5);
                File.WriteAllText(Path.Combine(deepestAllowed, "target.cs"), "// test");

                bool result = InvokeDoesFilesMatch(root, "[**/*.cs]");

                Assert.IsTrue(result);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void DoesFilesMatch_DoesNotTraversePastMaxDepth()
        {
            string root = CreateTempRoot();

            try
            {
                string beyondDepthLimit = CreateNestedDirectory(root, 6);
                File.WriteAllText(Path.Combine(beyondDepthLimit, "target.cs"), "// test");

                bool result = InvokeDoesFilesMatch(root, "[**/*.cs]");

                Assert.IsFalse(result);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void DoesFilesMatch_IgnoresConfiguredPaths()
        {
            string root = CreateTempRoot();

            try
            {
                string ignored = Directory.CreateDirectory(Path.Combine(root, "node_modules")).FullName;
                File.WriteAllText(Path.Combine(ignored, "found.cs"), "// test");

                bool result = InvokeDoesFilesMatch(root, "[*.cs]");

                Assert.IsFalse(result);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static bool InvokeDoesFilesMatch(string folder, string pattern)
        {
            Type validatorType = typeof(EditorConfigDocument).Assembly.GetType("EditorConfig.EditorConfigValidator", true);
            MethodInfo method = validatorType.GetMethod(
                "DoesFilesMatch",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                [typeof(string), typeof(string), typeof(string)],
                null);

            Assert.IsNotNull(method);

            object result = method.Invoke(null, [folder, pattern, null]);
            return (bool)result;
        }

        private static string CreateTempRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "EditorConfigLanguageTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static string CreateNestedDirectory(string root, int depth)
        {
            string current = root;

            for (int i = 0; i < depth; i++)
            {
                current = Directory.CreateDirectory(Path.Combine(current, $"d{i}")).FullName;
            }

            return current;
        }
    }
}
