using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace EditorConfigLanguage.Benchmarks.Support
{
    /// <summary>Centralizes reflection needed to benchmark internal production APIs.</summary>
    internal static class ProductionApi
    {
        private static readonly Assembly EditorConfigAssembly = typeof(EditorConfig.ParseItem).Assembly;

        private static readonly Type DocumentType =
            EditorConfigAssembly.GetType("EditorConfig.EditorConfigDocument", throwOnError: true);

        private static readonly MethodInfo CreateForTestMethod =
            DocumentType.GetMethod("CreateForTest", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("EditorConfig.EditorConfigDocument", "CreateForTest");

        private static readonly PropertyInfo IsParsingProperty =
            DocumentType.GetProperty("IsParsing", BindingFlags.Public | BindingFlags.Instance);

        private static readonly PropertyInfo ParentProperty =
            DocumentType.GetProperty("Parent", BindingFlags.Public | BindingFlags.Instance);

        private static readonly PropertyInfo ParseItemsProperty =
            DocumentType.GetProperty("ParseItems", BindingFlags.Public | BindingFlags.Instance);

        private static readonly PropertyInfo SectionsProperty =
            DocumentType.GetProperty("Sections", BindingFlags.Public | BindingFlags.Instance);

        private static readonly Type NamingEntityIndexType =
            EditorConfigAssembly.GetType("EditorConfig.NamingEntityIndex", throwOnError: true);

        private static readonly MethodInfo CreateNamingEntityIndexMethod =
            NamingEntityIndexType.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("EditorConfig.NamingEntityIndex", "Create");

        private static readonly PropertyInfo NamingEntitiesProperty =
            DocumentType.GetProperty("NamingEntities", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMemberException("EditorConfig.EditorConfigDocument", "NamingEntities");

        private static readonly Type NamingCompletionServiceType =
            EditorConfigAssembly.GetType("EditorConfig.NamingCompletionService", throwOnError: true);

        private static readonly MethodInfo GetNamingCompletionContextMethod =
            NamingCompletionServiceType.GetMethod("GetContext", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("EditorConfig.NamingCompletionService", "GetContext");

        private static readonly Type SchemaCatalogType =
            EditorConfigAssembly.GetType("EditorConfig.SchemaCatalog", throwOnError: true);

        private static readonly MethodInfo ParseSchemaMethod =
            SchemaCatalogType.GetMethod("ParseJson", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("EditorConfig.SchemaCatalog", "ParseJson");

        private static readonly Type AnalyzerConfigType =
            EditorConfigAssembly.GetType("EditorConfig.AnalyzerConfig", throwOnError: true);

        private static readonly MethodInfo TryCreateSectionNameMatcherMethod =
            AnalyzerConfigType.GetMethod("TryCreateSectionNameMatcher", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("EditorConfig.AnalyzerConfig", "TryCreateSectionNameMatcher");

        private static readonly Type SectionNameMatcherType =
            AnalyzerConfigType.GetNestedType("SectionNameMatcher", BindingFlags.NonPublic)
            ?? throw new MissingMemberException("EditorConfig.AnalyzerConfig", "SectionNameMatcher");

        private static readonly MethodInfo IsMatchMethod =
            SectionNameMatcherType.GetMethod("IsMatch", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMethodException("EditorConfig.AnalyzerConfig.SectionNameMatcher", "IsMatch");

        public static object CreateDocument(ITextBuffer buffer, string fileName)
            => CreateForTestMethod.Invoke(null, [buffer, fileName]);

        public static bool IsParsing(object document) => (bool)IsParsingProperty.GetValue(document);

        /// <summary>Blocks (with short sleeps) until the document finishes its current parse pass.</summary>
        public static void WaitForParse(object document, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            var spin = new SpinWait();

            while (IsParsing(document))
            {
                if (stopwatch.Elapsed > timeout)
                    throw new TimeoutException("EditorConfigDocument did not finish parsing within the allotted timeout.");

                spin.SpinOnce();
            }
        }

        public static object GetParent(object document) => ParentProperty.GetValue(document);

        public static int GetParseItemCount(object document) => ((ICollection)ParseItemsProperty.GetValue(document)).Count;

        public static int GetSectionCount(object document) => ((ICollection)SectionsProperty.GetValue(document)).Count;

        public static void LoadSchema()
            => ParseSchemaMethod.Invoke(null, [Path.Combine(AppContext.BaseDirectory, "Schema", "EditorConfig.json")]);

        public static object CreateNamingIndex(object document)
        {
            var properties = ((EditorConfig.EditorConfigDocument)document).Sections
                .SelectMany(section => section.Properties)
                .ToArray();
            return CreateNamingEntityIndexMethod.Invoke(null, [properties]);
        }

        public static object CompleteNamingEntityName(object document, string lineText)
        {
            object index = NamingEntitiesProperty.GetValue(document);
            Array indexes = Array.CreateInstance(NamingEntityIndexType, 1);
            indexes.SetValue(index, 0);
            return GetNamingCompletionContextMethod.Invoke(null, [lineText, lineText.Length, indexes]);
        }

        public static void Dispose(object document) => ((IDisposable)document).Dispose();

        public static object CreateMatcher(string sectionPattern)
            => TryCreateSectionNameMatcherMethod.Invoke(null, [sectionPattern]);

        public static bool IsMatch(object matcher, string candidatePath)
            => (bool)IsMatchMethod.Invoke(matcher, [candidatePath]);
    }
}
