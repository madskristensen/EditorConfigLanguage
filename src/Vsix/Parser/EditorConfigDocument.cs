using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

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

        private ITextBuffer _buffer;

        private EditorConfigDocument(ITextBuffer buffer)
        {
            _buffer = buffer;

            ParseItems = Parse(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length));
        }

        public List<ParseItem> ParseItems { get; } = new List<ParseItem>();

        public bool IsRoot
        {
            get
            {
                var first = ParseItems.FirstOrDefault(p => p.ItemType != ItemType.Comment);
                return first?.Text == "root";
            }
        }

        public static EditorConfigDocument FromTextBuffer(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigDocument(buffer));
        }

        public IEnumerable<ParseItem> ItemsInSpan(Span span)
        {
            return ParseItems.Where(i => span.Contains(i.Span));
        }

        public ParseItem FromPoint(SnapshotPoint point)
        {
            return ParseItems.FirstOrDefault(p => p.Span.Contains(point - 1));
        }

        public List<ParseItem> Parse(SnapshotSpan span)
        {
            var startLine = span.Snapshot.GetLineFromPosition(span.Start);
            var items = new List<ParseItem>();

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
                        }
                    }
            }

            ParseItems.Clear();//.RemoveAll(i => span.IntersectsWith(i.Span));
            ParseItems.AddRange(items);
            //ParseItems.Sort((a, b) => { try { return a.Span.Start.CompareTo(b.Span.Start); } catch { return 0; } });

            Validate();

            return items;
        }
    }
}
