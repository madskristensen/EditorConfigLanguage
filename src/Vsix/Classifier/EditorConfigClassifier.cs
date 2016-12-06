using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace EditorConfig
{
    internal class EditorConfigClassifier : IClassifier
    {
        private static Regex _rxValue = new Regex(@"(?<=\=([\s]+)?)([^\s:]+)", RegexOptions.Compiled);
        private static Regex _rxSeverity = new Regex(@"(?<==[^:]+:)[^\s]+", RegexOptions.Compiled);
        private static Regex _rxKeyword = new Regex(@"^([^=]+)\b(?=\=?)", RegexOptions.Compiled);
        private static Regex _rxHeader = new Regex(@"\[([^\]]+)\]", RegexOptions.Compiled); // [lib/**.js]
        private static Regex _rxComment = new Regex(@"#.*", RegexOptions.Compiled); // # comment
        private static List<Tuple<Regex, IClassificationType>> _map;

        public EditorConfigClassifier(IClassificationTypeRegistryService registry)
        {
            if (_map == null)
                _map = new List<Tuple<Regex, IClassificationType>>
                {
                    {Tuple.Create(_rxComment, registry.GetClassificationType(EditorConfigClassificationTypes.Comment))},
                    {Tuple.Create(_rxHeader, registry.GetClassificationType(EditorConfigClassificationTypes.Header))},
                    {Tuple.Create(_rxKeyword, registry.GetClassificationType(EditorConfigClassificationTypes.Keyword))},
                    {Tuple.Create(_rxValue, registry.GetClassificationType(EditorConfigClassificationTypes.Value))},
                    {Tuple.Create(_rxSeverity, registry.GetClassificationType(EditorConfigClassificationTypes.Severity))},
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
                    var matchSpan = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, match.Length);

                    // Make sure we don't double classify
                    if (!list.Any(s => s.Span.IntersectsWith(matchSpan)))
                        list.Add(new ClassificationSpan(matchSpan, tuple.Item2));

                    // No need to continue if the whole line has been classified
                    if (matchSpan.End.Position == line.End.Position)
                        return list;
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
