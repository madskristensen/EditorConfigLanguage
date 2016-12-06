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
        private static IEnumerable<Tuple<string, IClassificationType>> _map;

        public EditorConfigClassifier(IClassificationTypeRegistryService registry)
        {
            _map = _map ?? new[] {
                Tuple.Create(@"(#|;).+", registry.GetClassificationType(EditorConfigClassificationTypes.Comment)),
                Tuple.Create(@"\[([^\]]+)\]", registry.GetClassificationType(EditorConfigClassificationTypes.Section)),
                Tuple.Create(@"^([^=]+)\b(?=\=?)", registry.GetClassificationType(EditorConfigClassificationTypes.Keyword)),
                Tuple.Create(@"(?<=\=([\s]+)?)([^\s:]+)", registry.GetClassificationType(EditorConfigClassificationTypes.Value)),
                Tuple.Create(@"(?<==[^:]+:)[^\s]+", registry.GetClassificationType(EditorConfigClassificationTypes.Severity)),
            };
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();
            var line = span.Start.GetContainingLine();
            string text = line.GetText();

            if (string.IsNullOrWhiteSpace(text))
                return list;

            foreach (var tuple in _map)
                foreach (Match match in Regex.Matches(text, tuple.Item1))
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
