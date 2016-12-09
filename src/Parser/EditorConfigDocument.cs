using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private EditorConfigDocument(ITextBuffer buffer)
        {
            TextBuffer = buffer;
            TextBuffer.Changed += BufferChangedAsync;

            VsHelpers.SatisfyImportsOnce(this);
            ThreadHelper.JoinableTaskFactory.RunAsync(() => ParseAsync());
        }

        private async void BufferChangedAsync(object sender, EventArgs e)
        {
            await ParseAsync();
        }

        public ITextBuffer TextBuffer { get; }

        public List<ParseItem> ParseItems { get; } = new List<ParseItem>();

        public bool IsRoot
        {
            get
            {
                var prop = ParseItems.FirstOrDefault(p => p.ItemType != ItemType.Comment);
                var value = ParseItems.FirstOrDefault(p => p.Span.Start > prop?.Span.Start);

                return string.Equals(prop?.Text, Property.Root, StringComparison.OrdinalIgnoreCase) && string.Equals(value?.Text, "true", StringComparison.OrdinalIgnoreCase);
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
