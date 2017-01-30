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

            var item = _document.ItemAtPosition(point.Value);

            if (item == null)
                return;

            applicableToSpan = point.Value.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeNegative);

            if (item.Errors.Any())
            {
                foreach (var error in item.Errors)
                {
                    qiContent.Add(new Shared.EditorTooltip(error));
                    return;
                }
            }

            var property = _document.PropertyAtPosition(point.Value);

            if (!SchemaCatalog.TryGetKeyword(property?.Keyword?.Text, out Keyword keyword))
                return;

            if (item.ItemType == ItemType.Keyword)
            {
                qiContent.Add(new Shared.EditorTooltip(keyword));
            }
            else if (item.ItemType == ItemType.Value)
            {
                var value = keyword.Values.FirstOrDefault(v => v.Name.Equals(item.Text, StringComparison.OrdinalIgnoreCase));

                if (value != null && !string.IsNullOrEmpty(value.Description))
                    qiContent.Add(new Shared.EditorTooltip(value));
            }
            else if (item.ItemType == ItemType.Severity && SchemaCatalog.TryGetSeverity(item.Text, out Severity severity))
            {
                qiContent.Add(new Shared.EditorTooltip(severity));
            }
        }

        public void Dispose()
        {
        }
    }
}
