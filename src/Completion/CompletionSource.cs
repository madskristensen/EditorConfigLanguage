using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    class EditorConfigCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private EditorConfigDocument _document;
        private ITextStructureNavigatorSelectorService _navigator;
        private bool _disposed = false;

        public EditorConfigCompletionSource(ITextBuffer buffer, ITextStructureNavigatorSelectorService navigator)
        {
            _buffer = buffer;
            _navigator = navigator;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed || _document.IsParsing)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(snapshot);

            if (triggerPoint == null || !triggerPoint.HasValue)
                return;

            var line = triggerPoint.Value.GetContainingLine().Extent;
            var list = new List<Completion4>();
            var position = triggerPoint.Value.Position;
            var applicableTo = snapshot.CreateTrackingSpan(position, 0, SpanTrackingMode.EdgeInclusive);

            var prev = _document.ParseItems.LastOrDefault(p => p.Span.Start < position && !p.Span.Contains(position - 1));
            var parseItem = _document.ItemAtPosition(position);
            string moniker = null;

            // Property
            if (string.IsNullOrWhiteSpace(line.GetText()) || parseItem?.ItemType == ItemType.Keyword)
            {
                var isInRoot = !_document.ParseItems.Exists(p => p.ItemType == ItemType.Section && p.Span.Start < position);
                var items = isInRoot ? SchemaCatalog.Keywords : SchemaCatalog.Keywords.Where(i => i.Name != SchemaCatalog.Root);

                foreach (var property in items)
                    list.Add(CreateCompletion(property, property.Category));

                moniker = "keyword";
            }

            // Value
            else if (parseItem?.ItemType == ItemType.Value)
            {
                if (SchemaCatalog.TryGetKeyword(prev.Text, out Keyword item))
                {
                    foreach (var value in item.Values)
                        list.Add(CreateCompletion(value, iconAutomation: "value"));
                }

                moniker = "value";
            }

            // Severity
            else if ((position > 0 && snapshot.Length > 1 && snapshot.GetText(position - 1, 1) == ":") || parseItem?.ItemType == ItemType.Severity)
            {
                if (parseItem?.ItemType == ItemType.Unknown) // Colon was typed
                {
                    AddSeverity(list);
                }
                else
                {
                    var prop = _document.PropertyAtPosition(position);
                    if (SchemaCatalog.TryGetKeyword(prop?.Keyword?.Text, out Keyword key) && key.RequiresSeverity)
                        AddSeverity(list);
                }
                moniker = "severity";

            }

            if (!list.Any())
            {
                if (SchemaCatalog.TryGetKeyword(prev?.Text, out Keyword property))
                {
                    var eq = line.GetText().IndexOf("=");

                    if (eq != -1)
                    {
                        var eqPos = eq + line.Start.Position;

                        if (position > eqPos)
                            foreach (var value in property.Values)
                                list.Add(CreateCompletion(value));
                    }

                    moniker = "value";
                }
            }
            else
            {
                var trackingSpan = FindTokenSpanAtPosition(session);
                var span = trackingSpan.GetSpan(snapshot);
                var text = span.GetText();

                if (text == ":")
                    applicableTo = snapshot.CreateTrackingSpan(new Span(span.Start + 1, 0), SpanTrackingMode.EdgeInclusive);
                else if (!string.IsNullOrWhiteSpace(text))
                    applicableTo = trackingSpan;
            }

            CreateCompletionSet(moniker, completionSets, list, applicableTo);
        }

        private static void CreateCompletionSet(string moniker, IList<CompletionSet> completionSets, List<Completion4> list, ITrackingSpan applicableTo)
        {
            if (list.Any())
            {
                if (moniker == "keyword")
                {
                    var filters = new[] {
                        new IntellisenseFilter(KnownMonikers.Property, "Standard rules (Alt + S)", "s", Category.Standard.ToString()),
                        new IntellisenseFilter(KnownMonikers.CSFileNode, ".NET analysis rules (Alt + C)", "c", Category.CSharp.ToString()),
                        new IntellisenseFilter(KnownMonikers.DotNET, "C# analysis rules (Alt + D)", "d", Category.DotNet.ToString()),
                    };

                    completionSets.Add(new FilteredCompletionSet(moniker, applicableTo, list, Enumerable.Empty<Completion4>(), filters));
                }
                else
                {
                    completionSets.Add(new FilteredCompletionSet(moniker, applicableTo, list, Enumerable.Empty<Completion4>(), null));
                }
            }
        }

        private void AddSeverity(List<Completion4> list)
        {
            foreach (var severity in SchemaCatalog.Severities)
            {
                list.Add(CreateCompletion(severity));
            }
        }

        private Completion4 CreateCompletion(ITooltip item, Category category = Category.None, string iconAutomation = null)
        {
            IEnumerable<CompletionIcon2> icon = null;
            string automationText = iconAutomation;
            string text = item.Name;

            if (int.TryParse(item.Name, out int integer))
                text = "<integer>";

            if (!item.IsSupported)
            {
                icon = new[] { new CompletionIcon2(KnownMonikers.IntellisenseWarning, "warning", "") };
            }

            if (category != Category.None)
            {
                automationText = category.ToString();
            }

            var completion = new Completion4(text, item.Name, item.Description, item.Moniker, automationText, icon);
            completion.Properties.AddProperty("item", item);

            return completion;
        }

        private ITrackingSpan FindTokenSpanAtPosition(ICompletionSession session)
        {
            int offset = _buffer.CurrentSnapshot.Length > 0 ? -1 : 0;
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) + offset;
            ITextStructureNavigator navigator = _navigator.GetTextStructureNavigator(_buffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}