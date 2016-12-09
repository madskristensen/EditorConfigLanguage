using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    public class FilteredCompletionSet : CompletionSet2
    {
        public FilteredObservableCollection<Completion> currentCompletions;
        private BulkObservableCollection<Completion> _completions = new BulkObservableCollection<Completion>();
        public List<string> _activeFilters = new List<string>();
        private string _typed;

        public FilteredCompletionSet(ITrackingSpan applicableTo, IEnumerable<Completion> completions, IEnumerable<Completion> completionBuilders, IReadOnlyList<IIntellisenseFilter> filters)
            : base("All", "All", applicableTo, completions, completionBuilders, filters)
        {
            _completions.AddRange(completions);
            currentCompletions = new FilteredObservableCollection<Completion>(_completions);
        }

        public override IList<Completion> Completions
        {
            get { return currentCompletions; }
        }

        public override void Filter()
        {
            // This is handled in SelectBestMatch
        }

        public override void SelectBestMatch()
        {
            _typed = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
            var currentActiveFilters = Filters;

            if (currentActiveFilters != null && currentActiveFilters.Count > 0)
            {
                var activeFilters = currentActiveFilters.Where(f => f.IsChecked).Select(f => f.AutomationText);

                if (!activeFilters.Any())
                    activeFilters = currentActiveFilters.Select(f => f.AutomationText);

                _activeFilters.Clear();
                _activeFilters.AddRange(activeFilters);

                currentCompletions.Filter(new Predicate<Completion>(DoesCompletionMatchAutomationText));
            }

            SelectBestMatch(CompletionMatchType.MatchDisplayText, false);
        }

        private bool DoesCompletionMatchAutomationText(Completion completion)
        {
            return _activeFilters.Exists(x =>
                x.Equals(completion.IconAutomationText, StringComparison.OrdinalIgnoreCase)) &&
                completion.DisplayText.IndexOf(_typed, StringComparison.OrdinalIgnoreCase) > -1;
        }

        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
            int index = displayText.IndexOf(_typed, 0, StringComparison.OrdinalIgnoreCase);

            if (index > -1 && displayText.Length >= index + _typed.Length)
            {
                return new[] { Span.FromBounds(index, index + _typed.Length) };
            }

            return new List<Span>();
        }
    }
}
