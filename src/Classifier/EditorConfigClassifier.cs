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
        private EditorConfigDocument _document;
        private ITextBuffer _buffer;

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
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();

            if (_document.IsParsing)
                return list;

            IEnumerable<ParseItem> parseItems = _document.ItemsInSpan(span);

            foreach (ParseItem item in parseItems)
            {
                if (_map.ContainsKey(item.ItemType))
                {
                    var snapshotSpan = new SnapshotSpan(span.Snapshot, item.Span);
                    list.Add(new ClassificationSpan(snapshotSpan, _map[item.ItemType]));
                }
            }

            return list;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}
