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
        private static IClassificationType _duplicate, _noMatches;
        private string _dupeProp = PredefinedErrors.Codes.DuplicateProperty.Code;
        private string _dupeParent = PredefinedErrors.Codes.ParentDuplicateProperty.Code;
        private string _dupeSection = PredefinedErrors.Codes.DuplicateSection.Code;

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
            _noMatches = _noMatches ?? registry.GetClassificationType(EditorConfigClassificationTypes.NoMatches);

            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);

            _validator = EditorConfigValidator.FromDocument(_document);
            _validator.Validated += DocumentValiated;
        }

        private void DocumentValiated(object sender, EventArgs e)
        {
            if (ClassificationChanged == null)
                return;

            var duplicates = _document.ParseItems.Where(p => p.Errors.Any(err => err.ErrorCode == _dupeProp || err.ErrorCode == _dupeParent || err.ErrorCode == _dupeSection));

            foreach (var item in duplicates)
            {
                ClassificationChanged?.Invoke(this,
                new ClassificationChangedEventArgs(
                new SnapshotSpan(_buffer.CurrentSnapshot, item.Span)));
            }
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

                    if (item.Errors.Any(e => e.ErrorCode == _dupeProp || e.ErrorCode == _dupeParent))
                        list.Add(new ClassificationSpan(snapshotSpan, _duplicate));
                    else if (item.Errors.Any(e => e.ErrorCode == _dupeSection))
                        list.Add(new ClassificationSpan(snapshotSpan, _noMatches));
                    else
                        list.Add(new ClassificationSpan(snapshotSpan, _map[item.ItemType]));
                }
            }

            return list;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
