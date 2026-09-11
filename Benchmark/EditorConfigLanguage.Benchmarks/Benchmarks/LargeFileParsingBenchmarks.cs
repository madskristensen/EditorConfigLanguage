using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using EditorConfigLanguage.Benchmarks.Support;
using EditorConfigLanguage.Benchmarks.TestDoubles;

namespace EditorConfigLanguage.Benchmarks.Benchmarks
{
    /// <summary>Measures cold parsing of generated large .editorconfig files.</summary>
    [MemoryDiagnoser]
    public class LargeFileParsingBenchmarks
    {
        [Params(200, 1000, 5000)]
        public int SectionCount { get; set; }

        private string _content;

        [GlobalSetup]
        public void Setup() => _content = GenerateLargeEditorConfig(SectionCount);

        [Benchmark]
        public int ParseLargeFile()
        {
            var buffer = new FakeTextBuffer(_content);
            object document = ProductionApi.CreateDocument(buffer, @"X:\bench\large\.editorconfig");

            try
            {
                ProductionApi.WaitForParse(document, TimeSpan.FromSeconds(30));
                return ProductionApi.GetParseItemCount(document);
            }
            finally
            {
                ProductionApi.Dispose(document);
            }
        }

        private static string GenerateLargeEditorConfig(int sectionCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("root = true");
            sb.AppendLine();

            for (int i = 0; i < sectionCount; i++)
            {
                sb.AppendLine($"# section {i}");
                sb.AppendLine($"[*.section{i}.{{cs,vb}}]");
                sb.AppendLine("indent_style = space");
                sb.AppendLine("indent_size = 4");
                sb.AppendLine("trim_trailing_whitespace = true");
                sb.AppendLine("insert_final_newline = true");
                sb.AppendLine($"dotnet_diagnostic.CA{1000 + (i % 500)}.severity = warning:suggestion");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
