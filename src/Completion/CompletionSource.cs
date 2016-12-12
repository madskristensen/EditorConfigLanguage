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

            // Property
            if (string.IsNullOrWhiteSpace(line.GetText()) || parseItem?.ItemType == ItemType.Property)
            {
                var isInRoot = !_document.ParseItems.Exists(p => p.ItemType == ItemType.Section && p.Span.Start < position);
                var items = isInRoot ? SchemaCatalog.Properties : SchemaCatalog.Properties.Where(i => i.Name != SchemaCatalog.Root);

                foreach (var property in items)
                    list.Add(CreateCompletion(property, property.Category));
            }

            // Value
            else if (parseItem?.ItemType == ItemType.Value)
            {
                if (SchemaCatalog.TryGetProperty(prev.Text, out Keyword item))
                {
                    foreach (var value in item.Values)
                        list.Add(CreateCompletion(value));
                }
            }

            // Severity
            else if ((position > 0 && snapshot.Length > 1 && snapshot.GetText(position - 1, 1) == ":") || parseItem?.ItemType == ItemType.Severity)
            {
                if (SchemaCatalog.TryGetProperty(prev?.Prev.Text, out Keyword prop) && prop.SupportsSeverity)
                    AddSeverity(list);
            }

            if (!list.Any())
            {
                if (SchemaCatalog.TryGetProperty(prev?.Text, out Keyword property))
                {
                    var eq = line.GetText().IndexOf("=");

                    if (eq != -1)
                    {
                        var eqPos = eq + line.Start.Position;

                        if (triggerPoint.Value.Position > eqPos)
                            foreach (var value in property.Values)
                                list.Add(CreateCompletion(value));
                    }
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

            CreateCompletionSet(completionSets, list, applicableTo);
        }

        private static void CreateCompletionSet(IList<CompletionSet> completionSets, List<Completion4> list, ITrackingSpan applicableTo)
        {
            if (list.Any())
            {
                if (list.All(c => string.IsNullOrEmpty(c.IconAutomationText)))
                {
                    completionSets.Add(new FilteredCompletionSet(applicableTo, list, Enumerable.Empty<Completion4>(), null));
                }
                else
                {
                    var filters = new[] {
                        new IntellisenseFilter(KnownMonikers.Property, "Standard rules (Alt + S)", "s", Category.Standard.ToString()),
                        new IntellisenseFilter(KnownMonikers.CSFileNode, ".NET analysis rules (Alt + C)", "c", Category.CSharp.ToString()),
                        new IntellisenseFilter(KnownMonikers.DotNET, "C# analysis rules (Alt + D)", "d", Category.DotNet.ToString()),
                    };

                    completionSets.Add(new FilteredCompletionSet(applicableTo, list, Enumerable.Empty<Completion4>(), filters));
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

        private Completion4 CreateCompletion(ISchemaItem item, Category category = Category.None)
        {
            IEnumerable<CompletionIcon2> icon = null;
            string automationText = null;

            if (!item.IsSupported)
            {
                icon = new[] { new CompletionIcon2(KnownMonikers.IntellisenseWarning, "warning", "") };
            }

            if (category != Category.None)
            {
                automationText = category.ToString();
            }

            return new Completion4(item.Name, item.Name, item.Description, item.Moniker, automationText, icon);
        }

        private ITrackingSpan FindTokenSpanAtPosition(ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
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