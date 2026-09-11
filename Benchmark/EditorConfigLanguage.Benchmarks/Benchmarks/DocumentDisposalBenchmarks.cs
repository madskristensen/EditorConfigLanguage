using System;
using BenchmarkDotNet.Attributes;
using EditorConfigLanguage.Benchmarks.Support;
using EditorConfigLanguage.Benchmarks.TestDoubles;

namespace EditorConfigLanguage.Benchmarks.Benchmarks
{
    /// <summary>Measures whether live text buffers retain disposed documents through event handlers.</summary>
    [MemoryDiagnoser]
    public class DocumentDisposalBenchmarks
    {
        private const string Content = "root = true\n\n[*.cs]\nindent_style = space\nindent_size = 4\n[*.{json,yml}]\nindent_size = 2\n";

        [Params(true, false)]
        public bool DisposeDocuments { get; set; }

        [Params(10, 100)]
        public int DocumentCount { get; set; }

        [Benchmark]
        public int CountRetainedDocuments()
        {
            var buffers = new FakeTextBuffer[DocumentCount];
            var documents = new WeakReference[DocumentCount];

            for (int i = 0; i < DocumentCount; i++)
            {
                buffers[i] = new FakeTextBuffer(Content);
                documents[i] = CreateDocumentReference(buffers[i], DisposeDocuments);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            int retained = 0;
            foreach (WeakReference document in documents)
            {
                if (document.IsAlive)
                    retained++;
            }

            GC.KeepAlive(buffers);

            int expected = DisposeDocuments ? 0 : DocumentCount;
            if (retained != expected)
                throw new InvalidOperationException($"Expected {expected} retained documents but found {retained}.");

            return retained;
        }

        private static WeakReference CreateDocumentReference(FakeTextBuffer buffer, bool dispose)
        {
            object document = ProductionApi.CreateDocument(buffer, @"X:\bench\disposal\.editorconfig");
            ProductionApi.WaitForParse(document, TimeSpan.FromSeconds(5));

            var reference = new WeakReference(document);
            if (dispose)
                ProductionApi.Dispose(document);

            return reference;
        }
    }
}
