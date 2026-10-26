using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;

namespace EditorConfig
{
    internal class EditorConfigClassifier : IClassifier
    {
        private static Dictionary<ItemType, IClassificationType> _map;
        private readonly EditorConfigDocument _document;
        private readonly ITextBuffer _buffer;

        public EditorConfigClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _map = _map ?? new Dictionary<ItemType, IClassificationType> {
                { ItemType.Comment, registry.GetClassificationType(PredefinedClassificationTypeNames.Comment)},
                { ItemType.Section, registry.GetClassificationType(PredefinedClassificationTypeNames.String)},
                { ItemType.Keyword, registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier)},
                { ItemType.Value, registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword)},
                { ItemType.Severity, registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition)},
                { ItemType.Suppression, registry.GetClassificationType(PredefinedClassificationTypeNames.ExcludedCode)},
            };

            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
            _document.Parsed += DocumentParsed;
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();

            if (!_document.TryGetParseResult(span.Snapshot, out EditorConfigDocument.ParseResult result))
                return list;

            foreach (ParseItem item in result.Items)
            {
                if (span.IntersectsWith(item.Span) && _map.TryGetValue(item.ItemType, out IClassificationType classificationType))
                {
                    var snapshotSpan = new SnapshotSpan(span.Snapshot, item.Span);
                    list.Add(new ClassificationSpan(snapshotSpan, classificationType));
                }
            }

            return list;
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
