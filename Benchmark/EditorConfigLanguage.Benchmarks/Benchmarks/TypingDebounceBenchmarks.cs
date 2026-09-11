using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using EditorConfigLanguage.Benchmarks.Support;
using EditorConfigLanguage.Benchmarks.TestDoubles;

namespace EditorConfigLanguage.Benchmarks.Benchmarks
{
    /// <summary>Measures repeated edits followed by the parser's 150 ms debounce window.</summary>
    [MemoryDiagnoser]
    public class TypingDebounceBenchmarks
    {
        private const string InitialContent = "root = true\n\n[*.cs]\nindent_style = space\nindent_size = 4\n";

        [Params(5, 20, 50)]
        public int KeystrokeCount { get; set; }

        [Benchmark]
        public void TypeThenSettle()
        {
            var buffer = new FakeTextBuffer(InitialContent);
            object document = ProductionApi.CreateDocument(buffer, @"X:\bench\typing\.editorconfig");

            try
            {
                ProductionApi.WaitForParse(document, TimeSpan.FromSeconds(5));

                var content = new StringBuilder(InitialContent);
                for (int i = 0; i < KeystrokeCount; i++)
                {
                    content.Append("dotnet_style_qualification_for_field = true:suggestion\n");
                    buffer.SimulateEdit(content.ToString());
                }

                // Only the final keystroke's parse request should survive the debounce window.
                ProductionApi.WaitForParse(document, TimeSpan.FromSeconds(5));
            }
            finally
            {
                ProductionApi.Dispose(document);
            }
        }
    }
}
