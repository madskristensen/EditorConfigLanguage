using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    internal class SeverityTagger : ITagger<SeverityTag>
    {
        private ITextBuffer _buffer;
        private EditorConfigDocument _document;

        public SeverityTagger(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
            _document.Parsed += DocumentParsed;            
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            TagsChanged(this,
                new SnapshotSpanEventArgs(
                new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<SeverityTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_document.IsParsing || spans.Count == 0 || !EditorConfigPackage.Options.ShowSeverityIcons)
                yield break;

            var items = _document.ItemsInSpan(spans[0]).Where(p => p.ItemType == ItemType.Severity);

            foreach (var item in items)
            {
                var span = new SnapshotSpan(_buffer.CurrentSnapshot, item.Span);
                yield return new TagSpan<SeverityTag>(span, new SeverityTag(item));
            }
        }
    }
}
