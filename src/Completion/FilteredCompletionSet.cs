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

            var ordered = currentCompletions.OrderByDescending(c => GetHighlightedSpansInDisplayText(c.DisplayText).Sum(s => s.Length));

            //var matches = currentCompletions.Where(c => GetHighlightedSpansInDisplayText(c.DisplayText).Any());
            if (ordered.Any())
            {
                SelectionStatus = new CompletionSelectionStatus(ordered.First(), ordered.Count() == 1, ordered.Count() == 1);
            }
            else
            {
                SelectBestMatch(CompletionMatchType.MatchDisplayText, false);
            }
        }

        private bool DoesCompletionMatchAutomationText(Completion completion)
        {
            return _activeFilters.Exists(x =>
                x.Equals(completion.IconAutomationText, StringComparison.OrdinalIgnoreCase));
        }

        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
            var matches = new Dictionary<int, Span>();
            string match = string.Empty;

            for (int i = 0; i < _typed.Length; i++)
            {
                char c = _typed[i];

                if (!displayText.Contains(match + c))
                {
                    match = string.Empty;
                }

                var current = match + c;

                if (displayText.Contains(current))
                {
                    var index = displayText.IndexOf(current);
                    var offset = 0;

                    if (index > 0)
                    {
                        index = displayText.IndexOf("_" + current);
                        offset = 1;
                    }

                    if (index > -1)
                        matches[index] = new Span(index + offset, current.Length);

                    match += c;
                }
            }

            return matches.Values.ToArray();
        }
    }
}
