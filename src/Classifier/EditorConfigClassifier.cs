using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    internal class EditorConfigClassifier : IClassifier
    {
        private static Dictionary<ItemType, IClassificationType> _map;
        private EditorConfigDocument _document;
        private EditorConfigValidator _validator;
        private ITextBuffer _buffer;
        private static IClassificationType _duplicate;

        public EditorConfigClassifier(IClassificationTypeRegistryService registry, ITextBuffer buffer)
        {
            _map = _map ?? new Dictionary<ItemType, IClassificationType> {
                { ItemType.Comment, registry.GetClassificationType(EditorConfigClassificationTypes.Comment)},
                { ItemType.Section, registry.GetClassificationType(EditorConfigClassificationTypes.Section)},
                { ItemType.Property, registry.GetClassificationType(EditorConfigClassificationTypes.Keyword)},
                { ItemType.Value, registry.GetClassificationType(EditorConfigClassificationTypes.Value)},
                { ItemType.Severity, registry.GetClassificationType(EditorConfigClassificationTypes.Severity)},
            };

            _duplicate = _duplicate ?? registry.GetClassificationType(EditorConfigClassificationTypes.Duplicate);

            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);

            _validator = EditorConfigValidator.FromDocument(_document);
            _validator.Validated += DocumentValiated;
        }

        private void DocumentValiated(object sender, EventArgs e)
        {
            ClassificationChanged?.Invoke(this,
                new ClassificationChangedEventArgs(
                new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length)));
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();

            if (_document.IsParsing)
                return list;

            var parseItems = _document.ItemsInSpan(span);

            foreach (var item in parseItems)
            {
                if (_map.ContainsKey(item.ItemType))
                {
                    var snapshotSpan = new SnapshotSpan(span.Snapshot, item.Span);

                    if (item.Errors.Any(e => e.ErrorCode == 103 || e.ErrorCode == 104))
                        list.Add(new ClassificationSpan(snapshotSpan, _duplicate));
                    else
                        list.Add(new ClassificationSpan(snapshotSpan, _map[item.ItemType]));
                }
            }

            return list;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
