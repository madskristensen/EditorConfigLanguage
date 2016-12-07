using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private ITextBuffer _buffer;
        private CancellationTokenSource _cancelToken;

        private EditorConfigDocument(ITextBuffer buffer)
        {
            _buffer = buffer;
            _buffer.PostChanged += BufferPostChangedAsync;
            _cancelToken = new CancellationTokenSource();

            ThreadHelper.JoinableTaskFactory.Run(() => ParseAsync(_cancelToken.Token));
        }

        private async void BufferPostChangedAsync(object sender, EventArgs e)
        {
            _cancelToken.Cancel();
            _cancelToken = new CancellationTokenSource();
            await ParseAsync(_cancelToken.Token);

        }

        public List<ParseItem> ParseItems { get; } = new List<ParseItem>();

        public bool IsRoot
        {
            get
            {
                var first = ParseItems.FirstOrDefault(p => p.ItemType != ItemType.Comment);
                return string.Equals(first?.Text, "root", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static EditorConfigDocument FromTextBuffer(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigDocument(buffer));
        }

        public IEnumerable<ParseItem> ItemsInSpan(Span span)
        {
            return ParseItems.Where(i => span.Contains(i.Span));
        }

        public ParseItem ItemAtPosition(int point)
        {
            return ParseItems.FirstOrDefault(p => p.Span.Contains(point - 1));
        }
    }
}
