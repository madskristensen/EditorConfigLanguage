using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private static IEnumerable<Tuple<string, ItemType>> _map = new[] {
                Tuple.Create(@"(#|;).+", ItemType.Comment),
                Tuple.Create(@"\[([^\]]+)\]", ItemType.Section),
                Tuple.Create(@"^([^=]+)\b(?=\=?)", ItemType.Keyword),
                Tuple.Create(@"(?<=\=([\s]+)?)([^\s:]+)", ItemType.Value),
                Tuple.Create(@"(?<==[^:]+:)[^\s]+", ItemType.Severity),
            };

        public List<ParseItem> Parse(SnapshotSpan span)
        {
            var startLine = span.Snapshot.GetLineFromPosition(span.Start);
            var items = new List<ParseItem>();
            ParseItem parent = null;

            foreach (var line in span.Snapshot.Lines.Where(l => l.LineNumber >= startLine.LineNumber))
            {
                string text = line.GetText();

                foreach (var tuple in _map)
                    foreach (Match match in Regex.Matches(text, tuple.Item1))
                    {
                        var matchSpan = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, match.Length);

                        // Make sure we don't double classify
                        if (!items.Any(s => s.Span.IntersectsWith(matchSpan)))
                        {
                            var textValue = matchSpan.GetText();
                            var item = new ParseItem(tuple.Item2, matchSpan, textValue);
                            items.Add(item);

                            if (parent != null && item.ItemType != ItemType.Section && item.ItemType != ItemType.Comment)
                                parent.AddChild(item);

                            if (tuple.Item2 == ItemType.Section)
                                parent = item;
                        }
                    }
            }

            ParseItems.RemoveAll(i => i.Span.Start >= startLine.Start);
            ParseItems.AddRange(items);
            //ParseItems.Sort((a, b) => { try { return a.Span.Start.CompareTo(b.Span.Start); } catch { return 0; } });

            Validate();

            return items;
        }
    }
}
