//using System;
//using System.Linq;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.Text;

//namespace EditorConfig
//{
//    public class FilteredCompletionSet : CompletionSet2
//    {
//        private List<Completion> _filteredCompletions;
//        private List<Completion> _completions;

//        public FilteredCompletionSet(ITrackingSpan applicableTo, IEnumerable<Completion3> completions, IEnumerable<Completion> completionBuilders, IReadOnlyList<IIntellisenseFilter> filters)
//            : base("All", "All", applicableTo, completions, completionBuilders, filters)
//        {
//            _completions = new List<Completion>(completions);
//            _filteredCompletions = new List<Completion>(completions);
//        }

//        public override IList<Completion> Completions
//        {
//            get
//            {
//                return _filteredCompletions ?? _completions;
//            }
//        }

//        public override void Filter()
//        {
//            var text = ApplicableTo.GetText(ApplicableTo.TextBuffer.CurrentSnapshot);
//            //var enabledFilters = Filters.Where(f => f.IsEnabled).Select(f => f.Moniker);

//            if (!string.IsNullOrWhiteSpace(text))
//            {
//                _filteredCompletions = _completions.Where(c => c.DisplayText.IndexOf(text, StringComparison.OrdinalIgnoreCase) > -1).ToList();
//            }

//            //if (enabledFilters.Any())
//            //{
//            //    var comp3 = _filteredCompletions.Cast<Completion3>().ToList();
//            //    _filteredCompletions.Clear();
//            //    _filteredCompletions.AddRange(comp3.Where(f => enabledFilters.Any(c => f.IconMoniker.Guid == c.Guid && f.IconMoniker.Id == c.Id)));
//            //}
//        }

//        public override void SelectBestMatch()
//        {
//            Filter();

//            //if (Completions.Any())
//            //{
//            //    SelectionStatus = new CompletionSelectionStatus(Completions.First(), true, false);
//            //}
//            //else
//            //{
//            base.SelectBestMatch(CompletionMatchType.MatchDisplayText, false);
//            //}
//        }
//    }
//}
