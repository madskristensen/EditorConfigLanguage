using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EditorConfig
{
    class SuggestedActionsSource : ISuggestedActionsSource
    {
        private Section _section;
        private EditorConfigDocument _document;

        public SuggestedActionsSource(ITextBuffer buffer)
        {
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
                var deleteSection = new DeleteSectionAction(range.Snapshot.TextBuffer, _section.Span);
                yield return new SuggestedActionSet(new[] { deleteSection });
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
