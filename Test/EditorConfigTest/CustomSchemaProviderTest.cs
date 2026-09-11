using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using EditorConfig;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace EditorConfigTest
{
    [TestClass]
    public class CustomSchemaProviderTest
    {
        [TestMethod]
        public void LoadKeywordsFromFile_PreservesAliasesAndValues()
        {
            string file = WriteSchema("""
                {
                  "properties": [
                    {
                      "name": "custom_property",
                      "description": "Custom",
                      "values": [ "one", "two" ],
                      "aliases": [ "legacy_property" ]
                    }
                  ]
                }
                """);

            try
            {
                List<Keyword> keywords = CustomSchemaProvider.LoadKeywordsFromFile(file);

                Assert.HasCount(1, keywords);
                Assert.AreEqual("custom_property", keywords[0].Name);
                CollectionAssert.AreEquivalent(new[] { "one", "two" }, keywords[0].Values.Select(value => value.Name).ToArray());
                CollectionAssert.AreEquivalent(new[] { "legacy_property" }, keywords[0].Aliases.ToArray());
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void LoadKeywordsFromFile_WithoutProperties_ReturnsEmpty()
        {
            string file = WriteSchema("""{ "name": "empty" }""");

            try
            {
                Assert.IsEmpty(CustomSchemaProvider.LoadKeywordsFromFile(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void LoadKeywordsFromFile_WithMalformedJson_Throws()
        {
            string file = WriteSchema("""{ "properties": [""");

            try
            {
                Assert.Throws<JsonReaderException>(() => CustomSchemaProvider.LoadKeywordsFromFile(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void FilterKeywords_EnforcesBuiltInAndFirstRegistrationPrecedence()
        {
            Keyword builtInConflict = CreateKeyword("INDENT_STYLE");
            Keyword first = CreateKeyword("custom_first");
            Keyword duplicate = CreateKeyword("CUSTOM_FIRST");
            var builtInNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "indent_style" };
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IReadOnlyList<Keyword> firstResult = CustomSchemaProvider.FilterKeywords(
                "First extension",
                KnownMonikers.Property,
                new[] { builtInConflict, first },
                builtInNames,
                seenNames);
            IReadOnlyList<Keyword> secondResult = CustomSchemaProvider.FilterKeywords(
                "Second extension",
                KnownMonikers.Class,
                new[] { duplicate },
                builtInNames,
                seenNames);

            Assert.HasCount(1, firstResult);
            Assert.AreSame(first, firstResult[0]);
            Assert.AreEqual("First extension", first.CustomExtensionName);
            Assert.AreEqual(KnownMonikers.Property, first.CustomMoniker);
            Assert.IsEmpty(secondResult);
        }

        [TestMethod]
        public void ParseMoniker_HandlesKnownRawAndFallbackFormats()
        {
            var guid = Guid.NewGuid();

            ImageMoniker known = CustomSchemaProvider.ParseMoniker("KnownMonikers.Class");
            ImageMoniker raw = CustomSchemaProvider.ParseMoniker($"{guid}:42");
            ImageMoniker fallback = CustomSchemaProvider.ParseMoniker("not-a-moniker");

            Assert.AreEqual(KnownMonikers.Class.Guid, known.Guid);
            Assert.AreEqual(KnownMonikers.Class.Id, known.Id);
            Assert.AreEqual(guid, raw.Guid);
            Assert.AreEqual(42, raw.Id);
            Assert.AreEqual(KnownMonikers.Property, fallback);
        }

        private static Keyword CreateKeyword(string name)
            => new(name, "Description", [], [], false, false, false, false, null, null, null);

        private static string WriteSchema(string contents)
        {
            string file = Path.Combine(Path.GetTempPath(), $"EditorConfigLanguage-{Guid.NewGuid():N}.json");
            File.WriteAllText(file, contents);
            return file;
        }
    }
}
