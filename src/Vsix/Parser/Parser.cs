//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.Text;

//namespace EditorConfig
//{
//    public static class Parser
//    {
//        private static IEnumerable<Tuple<string, ItemType>> _map = new[] {
//                Tuple.Create(@"(#|;).+", ItemType.Comment),
//                Tuple.Create(@"\[([^\]]+)\]", ItemType.Section),
//                Tuple.Create(@"^([^=]+)\b(?=\=?)", ItemType.Keyword),
//                Tuple.Create(@"(?<=\=([\s]+)?)([^\s:]+)", ItemType.Value),
//                Tuple.Create(@"(?<==[^:]+:)[^\s]+", ItemType.Severity),
//            };

//        public static EditorConfigDocument GetOrCreateEditorConfigDocument(this ITextBuffer buffer)
//        {
//            return buffer.Properties.GetOrCreateSingletonProperty(() => Parse(buffer));
//        }

//        public static EditorConfigDocument Parse(ITextBuffer buffer)
//        {
//            var doc = new EditorConfigDocument();

//            foreach (var line in buffer.CurrentSnapshot.Lines)
//            {
//                string text = line.GetText();

//                foreach (var tuple in _map)
//                    foreach (Match match in Regex.Matches(text, tuple.Item1))
//                    {
//                        var matchSpan = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, match.Length);

//                        // Make sure we don't double classify
//                        if (!doc.ParseItems.Any(s => s.Span.IntersectsWith(matchSpan)))
//                        {
//                            var textValue = matchSpan.GetText();
//                            var item = new ParseItem(tuple.Item2, matchSpan, textValue);
//                            doc.ParseItems.Add(item);
//                        }
//                    }
//            }

//            return doc;
//        }
//    }
//}
