using System;
using System.Text;

using BenchmarkDotNet.Attributes;

using EditorConfigLanguage.Benchmarks.Support;
using EditorConfigLanguage.Benchmarks.TestDoubles;

namespace EditorConfigLanguage.Benchmarks.Benchmarks
{
    /// <summary>Measures naming-index construction and completion lookup as naming configurations grow.</summary>
    [MemoryDiagnoser]
    public class NamingEntityBenchmarks
    {
        private const string CompletionLine = "dotnet_naming_style.";
        private object _document;

        [Params(25, 250, 1000)]
        public int EntityCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            ProductionApi.LoadSchema();
            var buffer = new FakeTextBuffer(GenerateNamingConfiguration(EntityCount));
            _document = ProductionApi.CreateDocument(buffer, @"X:\bench\naming\.editorconfig");
            ProductionApi.WaitForParse(_document, TimeSpan.FromSeconds(30));
        }

        [GlobalCleanup]
        public void Cleanup() => ProductionApi.Dispose(_document);

        [Benchmark]
        public object BuildNamingIndex() => ProductionApi.CreateNamingIndex(_document);

        [Benchmark]
        public object CompleteEntityName() => ProductionApi.CompleteNamingEntityName(_document, CompletionLine);

        private static string GenerateNamingConfiguration(int entityCount)
        {
            var content = new StringBuilder("[*.cs]\n");

            for (int i = 0; i < entityCount; i++)
            {
                content.AppendLine($"dotnet_naming_style.style_{i}.capitalization = pascal_case");
                content.AppendLine($"dotnet_naming_style.style_{i}.required_prefix = _");
                content.AppendLine($"dotnet_naming_rule.rule_{i}.style = style_{i}");
            }

            return content.ToString();
        }
    }
}
