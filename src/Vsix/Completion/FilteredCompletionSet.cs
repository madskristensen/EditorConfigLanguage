using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    public class FilteredCompletionSet : CompletionSet2
    {
        public FilteredObservableCollection<Completion> currentCompletions;
        private BulkObservableCollection<Completion> _completions = new BulkObservableCollection<Completion>();
        public List<string> _filterBufferText = new List<string>();
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

        public override void SelectBestMatch()
        {
            if (Filters != null && Filters.Any())
            {
                var enabledFilters = Filters.Where(f => f.IsChecked).Select(f => f.AutomationText).Distinct();

                if (!enabledFilters.Any())
                    enabledFilters = Filters.Select(f => f.AutomationText).Distinct();

                _filterBufferText.Clear();
                _filterBufferText.AddRange(enabledFilters);

                _typed = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);

                currentCompletions.Filter(new Predicate<Completion>(DoesCompletionMatchAutomationText));
            }

            base.SelectBestMatch();
        }

        private bool DoesCompletionMatchAutomationText(Completion completion)
        {
            return (_filterBufferText.Exists(x =>
            x.Equals(completion.IconAutomationText, StringComparison.OrdinalIgnoreCase)) &&
            completion.DisplayText.Contains(_typed)
            );
        }
}
}
