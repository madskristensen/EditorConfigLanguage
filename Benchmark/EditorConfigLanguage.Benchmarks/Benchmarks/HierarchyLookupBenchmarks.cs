using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using EditorConfigLanguage.Benchmarks.Support;
using EditorConfigLanguage.Benchmarks.TestDoubles;

namespace EditorConfigLanguage.Benchmarks.Benchmarks
{
    /// <summary>Measures the ancestor-directory walk when no parent .editorconfig exists.</summary>
    [MemoryDiagnoser]
    public class HierarchyLookupBenchmarks
    {
        [Params(5, 25, 75)]
        public int Depth { get; set; }

        private string _fileName;

        [GlobalSetup]
        public void Setup()
        {
            string segments = string.Join("\\", Enumerable.Repeat("x", Depth));
            _fileName = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "EditorConfigLanguage.Benchmarks",
                segments,
                "source.cs");
        }

        [Benchmark]
        public object WalkParentChain()
        {
            var buffer = new FakeTextBuffer("[*.cs]\nindent_style = space\n");
            object document = ProductionApi.CreateDocument(buffer, _fileName);

            try
            {
                ProductionApi.WaitForParse(document, TimeSpan.FromSeconds(5));
                return ProductionApi.GetParent(document);
            }
            finally
            {
                ProductionApi.Dispose(document);
            }
        }
    }
}
