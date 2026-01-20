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
        private readonly ITextView _view;
        private readonly EditorConfigDocument _document;
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

                // Suppressions - materialize once to avoid double enumeration
                var itemsWithErrors = _document.ItemsInSpan(range).Where(p => p.HasErrors).ToList();
                if (itemsWithErrors.Count > 0)
                {
                    var actions = new List<SuppressErrorAction>();

                    foreach (ParseItem item in itemsWithErrors)
                    {
                        foreach (DisplayError error in item.Errors)
                        {
                            var action = new SuppressErrorAction(_document, error.Name);

                            if (action.IsEnabled)
                                actions.Add(action);
                        }
                    }
                    list.AddRange(CreateActionSet([.. actions]));
                }

                // Missing rules
                List<Keyword> missingRules = AddMissingRulesAction.FindMissingRulesAll(_document.GetAllIncludedRules());
                if (missingRules.Count != 0)
                {
                    var addMissingRules = new AddMissingRulesAction(missingRules, _document, _view);
                    list.AddRange(CreateActionSet(addMissingRules));
                }
            }

            return list;
        }

        public IEnumerable<SuggestedActionSet> CreateActionSet(params BaseSuggestedAction[] actions)
        {
            return new[] { new SuggestedActionSet(categoryName: null, actions: actions, title: null, priority: SuggestedActionSetPriority.None, applicableToSpan: null) };
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
