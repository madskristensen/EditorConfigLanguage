using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace EditorConfig
{
    partial class EditorConfigDocument: IDisposable
    {
        private static IEnumerable<Tuple<string, ItemType>> _map = new[] {
                Tuple.Create(@"(#|;).+", ItemType.Comment),
                Tuple.Create(@"\[([^\]]+)\]", ItemType.Section),
                Tuple.Create(@"^([^=]+)\b(?=\=?)", ItemType.Keyword),
                Tuple.Create(@"(?<=\=([\s]+)?)([^\s:]+)", ItemType.Value),
                Tuple.Create(@"(?<==[^:]+:)[^\s]+", ItemType.Severity),
            };

        public bool IsParsing { get; private set; }

        private async System.Threading.Tasks.Task ParseAsync(CancellationToken cancellationToken)
        {
            IsParsing = true;

            await System.Threading.Tasks.Task.Run(() =>
            {
                var items = new List<ParseItem>();
                ParseItem parent = null;

                foreach (var line in _buffer.CurrentSnapshot.Lines)
                {
                    string text = line.GetText();

                    foreach (var tuple in _map)
                        foreach (Match match in Regex.Matches(text, tuple.Item1))
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                IsParsing = false;
                                return;
                            }
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

                ParseItems.Clear();
                ParseItems.AddRange(items);

                Validate();
                IsParsing = false;

                Parsed?.Invoke(this, EventArgs.Empty);
            });
        }

        public void Dispose()
        {
            if (_cancelToken != null)
            {
                _cancelToken.Dispose();
            }
        }

        public event EventHandler Parsed;
    }
}
