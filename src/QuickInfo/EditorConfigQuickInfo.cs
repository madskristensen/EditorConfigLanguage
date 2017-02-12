using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
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

            ParseItem item = _document.ItemAtPosition(point.Value);

            if (item == null)
                return;

            applicableToSpan = point.Value.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeNegative);

            if (item.Errors.Any())
            {
                foreach (DisplayError error in item.Errors)
                {
                    qiContent.Add(new Shared.EditorTooltip(error));
                    return;
                }
            }

            Property property = _document.PropertyAtPosition(point.Value);

            SchemaCatalog.TryGetKeyword(property?.Keyword?.Text, out Keyword keyword);

            // Keyword
            if (keyword != null && item.ItemType == ItemType.Keyword)
            {
                qiContent.Add(new Shared.EditorTooltip(keyword));
            }

            // Value
            else if (keyword != null && item.ItemType == ItemType.Value)
            {
                Value value = keyword.Values.FirstOrDefault(v => v.Name.Is(item.Text));

                if (value != null && !string.IsNullOrEmpty(value.Description))
                    qiContent.Add(new Shared.EditorTooltip(value));
            }

            // Severity
            else if (item.ItemType == ItemType.Severity && SchemaCatalog.TryGetSeverity(item.Text, out Severity severity))
            {
                qiContent.Add(new Shared.EditorTooltip(severity));
            }

            // Suppression
            else if (item.ItemType == ItemType.Suppression && ErrorCatalog.TryGetErrorCode(item.Text, out var code))
            {
                qiContent.Add(new Shared.EditorTooltip(code));
            }
        }

        public void Dispose()
        {
        }
    }
}
