using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    internal class EditorConfigQuickInfo : IQuickInfoSource
    {
        private ITextBuffer _buffer;
        private EditorConfigDocument _document;

        public EditorConfigQuickInfo(ITextBuffer buffer)
        {
            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);

            if (session == null || qiContent == null || !point.HasValue || point.Value.Position >= point.Value.Snapshot.Length)
                return;

            var item = _document.ItemAtPosition(point.Value);

            if (item == null)
                return;

            if (item.Errors.Any())
            {
                foreach (var error in item.Errors)
                {
                    qiContent.Add(new Shared.EditorTooltip(error));
                    break;
                }
            }
            else if (item.ItemType == ItemType.Property && SchemaCatalog.TryGetProperty(item.Text, out Keyword property))
            {
                qiContent.Add(new Shared.EditorTooltip(property));
            }
            else if (item.ItemType == ItemType.Value)
            {
                var index = _document.ParseItems.IndexOf(item);

                if (index > 0)
                {
                    var prev = _document.ParseItems.ElementAt(index - 1);
                    if (prev.ItemType == ItemType.Property && SchemaCatalog.TryGetProperty(prev.Text, out Keyword keyword))
                    {
                        var value = keyword.Values.FirstOrDefault(v => v.Name == item.Text && !string.IsNullOrEmpty(v.Description));
                        if (value != null)
                            qiContent.Add(new Shared.EditorTooltip(value));
                    }
                }
            }
            else if (item.ItemType == ItemType.Severity && SchemaCatalog.TryGetSeverity(item.Text, out Severity severity))
            {
                qiContent.Add(new Shared.EditorTooltip(severity));
            }

            applicableToSpan = point.Value.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeNegative);
        }

        public void Dispose()
        {
        }
    }
}
