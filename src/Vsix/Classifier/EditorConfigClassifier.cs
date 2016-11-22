using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EditorConfig
{
    internal class EditorConfigClassifier : IClassifier
    {
        private static Regex _rxValue = new Regex(@"(?<=\=\s?)([^\s]+)", RegexOptions.Compiled);
        private static Regex _rxKeyword = new Regex(@"^([^=]+)\b(?=\=?)", RegexOptions.Compiled);
        private static Regex _rxHeader = new Regex(@"\[([^\]]+)\]", RegexOptions.Compiled); // [lib/**.js]
        private static Regex _rxComment = new Regex(@"#.*", RegexOptions.Compiled); // # comment
        private static List<Tuple<Regex, IClassificationType>> _map;

        public EditorConfigClassifier(IClassificationTypeRegistryService registry)
        {
            if (_map == null)
                _map = new List<Tuple<Regex, IClassificationType>>
                {
                    {Tuple.Create(_rxComment, registry.GetClassificationType(PredefinedClassificationTypeNames.Comment))},
                    {Tuple.Create(_rxHeader, registry.GetClassificationType(PredefinedClassificationTypeNames.String))},
                    {Tuple.Create(_rxKeyword, registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword))},
                    {Tuple.Create(_rxValue, registry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition))},
                };
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> list = new List<ClassificationSpan>();
            ITextSnapshotLine line = span.Start.GetContainingLine();
            string text = line.GetText();

            if (string.IsNullOrWhiteSpace(text))
                return list;

            foreach (var tuple in _map)
                foreach (Match match in tuple.Item1.Matches(text))
                {
                    var str = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, match.Length);

                    // Make sure we don't double classify
                    if (!list.Any(s => s.Span.IntersectsWith(str)))
                        list.Add(new ClassificationSpan(str, tuple.Item2));
                }

            return list;

        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}
