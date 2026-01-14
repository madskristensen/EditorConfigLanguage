using EditorConfig;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
