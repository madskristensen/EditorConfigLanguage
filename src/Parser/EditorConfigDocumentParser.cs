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
                Tuple.Create(@"^\s*\[([^#;]+)\]", ItemType.Section),
                Tuple.Create(@"^([^=]+)\b(?=\=?)", ItemType.Property),
                Tuple.Create(@"(?<=\=([\s]+)?)([^\s:]+)", ItemType.Value),
                Tuple.Create(@"(?<==[^:]+:)[^\s]+", ItemType.Severity),
            };

        public bool IsParsing { get; private set; }

        private  System.Threading.Tasks.Task ParseAsync()
        {
            IsParsing = true;

            return System.Threading.Tasks.Task.Run(() =>
            {
                var items = new List<ParseItem>();
                ParseItem parent = null;

                foreach (var line in TextBuffer.CurrentSnapshot.Lines)
                {
                    string text = line.GetText();

                    foreach (var tuple in _map)
                        foreach (Match match in Regex.Matches(text, tuple.Item1))
                        {
                            var matchSpan = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, match.Length);

                            // Make sure we don't double classify
                            if (items.Any(s => s.Span.IntersectsWith(matchSpan)))
                                continue;

                            var textValue = matchSpan.GetText();
                            var item = new ParseItem(tuple.Item2, matchSpan, textValue);
                            var prev = items.LastOrDefault();

                            if (prev != null)
                            {
                                item.Prev = prev;
                                prev.Next = item;
                            }

                            items.Add(item);

                            if (parent != null && item.ItemType != ItemType.Section && item.ItemType != ItemType.Comment)
                                parent.AddChild(item);

                            if (tuple.Item2 == ItemType.Section)
                                parent = item;
                        }
                }

                ParseItems.Clear();
                ParseItems.AddRange(items);

                Validate();
                IsParsing = false;

                Parsed?.Invoke(this, EventArgs.Empty);
            });
        }

        public event EventHandler Parsed;
    }
}
