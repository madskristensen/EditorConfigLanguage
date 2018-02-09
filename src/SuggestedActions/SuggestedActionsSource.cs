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

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            _section = _document.Sections.FirstOrDefault(s => s.Span.Contains(range.Start));

            return Task.FromResult(_section != null);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var list = new List<SuggestedActionSet>();

            if (_section != null)
            {
                var removeDuplicate = new RemoveDuplicatePropertiesAction(_section, _view);
                if (removeDuplicate.IsEnabled)
                    list.AddRange(CreateActionSet(removeDuplicate));

                var sortProperties = new SortPropertiesAction(_section, _view);
                var sortAllProperties = new SortAllPropertiesAction(_document, _view);
                list.AddRange(CreateActionSet(sortProperties, sortAllProperties));

                var deleteSection = new DeleteSectionAction(range.Snapshot.TextBuffer, _section);
                list.AddRange(CreateActionSet(deleteSection));

                // Suppressions
                IEnumerable<ParseItem> items = _document.ItemsInSpan(range).Where(p => p.HasErrors);
                if (items.Any())
                {
                    IEnumerable<DisplayError> errors = items.SelectMany(i => i.Errors);
                    var actions = new List<SuppressErrorAction>();

                    foreach (DisplayError error in errors)
                    {
                        var action = new SuppressErrorAction(_document, error.Name);

                        if (action.IsEnabled)
                            actions.Add(action);
                    }

                    list.AddRange(CreateActionSet(actions.ToArray()));
                }
            }

            return list;
        }

        public IEnumerable<SuggestedActionSet> CreateActionSet(params BaseSuggestedAction[] actions)
        {
            return new[] { new SuggestedActionSet(actions) };
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
