using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private ITextBuffer _buffer;

        private EditorConfigDocument(ITextBuffer buffer)
        {
            _buffer = buffer;
            ParseItems = Parse(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length));
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
