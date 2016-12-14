using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EditorConfig
{
    class SuggestedActionsSource : ISuggestedActionsSource
    {
        private ITextView _view;
        private EditorConfigDocument _document;
        private Section _section;

        public SuggestedActionsSource(ITextView view, ITextBuffer buffer)
        {
            _view = view;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(() =>
            {
                _section = _document.Sections.FirstOrDefault(s => s.Item.Span.Contains(range));

                return _section != null;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            if (_section != null)
            {
                var sortProperties = new SortPropertiesAction(_section, _view);
                var sortAllProperties = new SortAllPropertiesAction(_document, _view);
                yield return new SuggestedActionSet(new ISuggestedAction[] { sortProperties, sortAllProperties });

                var deleteSection = new DeleteSectionAction(range.Snapshot.TextBuffer, _section);
                yield return new SuggestedActionSet(new ISuggestedAction[] { deleteSection });
            }
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }


        public event EventHandler<EventArgs> SuggestedActionsChanged
        {
            add { }
            remove { }
        }
    }
}
