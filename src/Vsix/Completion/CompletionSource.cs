using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;

namespace EditorConfig
{
    class EditorConfigCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private IClassifier _classifier;
        private ITextStructureNavigatorSelectorService _navigator;
        private bool _disposed = false;

        public EditorConfigCompletionSource(ITextBuffer buffer, IClassifierAggregatorService classifier, ITextStructureNavigatorSelectorService navigator)
        {
            _buffer = buffer;
            _classifier = classifier.GetClassifier(buffer);
            _navigator = navigator;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(snapshot);

            if (triggerPoint == null || !triggerPoint.HasValue)
                return;

            var line = triggerPoint.Value.GetContainingLine().Extent;
            var list = new List<Completion4>();
            var applicableTo = snapshot.CreateTrackingSpan(triggerPoint.Value.Position, 0, SpanTrackingMode.EdgeInclusive);
            var position = triggerPoint.Value.Position;

            if (string.IsNullOrWhiteSpace(line.GetText()))
            {
                foreach (var key in Keyword.AllItems)
                    list.Add(CreateCompletion(key.Name, key.Moniker, key.Tag, key.IsSupported, key.Description));
            }
            else if (position > 0 && snapshot.Length > 1 && snapshot.GetText(position - 1, 1) == ":")
            {
                AddSeverity(list);
            }
            else
            {
                var spans = _classifier.GetClassificationSpans(line);
                SnapshotSpan extent = FindTokenSpanAtPosition(session).GetSpan(snapshot);
                string current = string.Empty;

                foreach (var span in spans)
                {
                    if (span.ClassificationType.IsOfType(EditorConfigClassificationTypes.Keyword))
                    {
                        current = span.Span.GetText();

                        if (!span.Span.Contains(extent))
                            continue;

                        foreach (var key in Keyword.AllItems)
                            list.Add(CreateCompletion(key.Name, key.Moniker, key.Tag, key.IsSupported, key.Description));
                    }
                    else if (span.ClassificationType.IsOfType(EditorConfigClassificationTypes.Value))
                    {
                        if (!span.Span.Contains(extent))
                            continue;

                        Keyword item = Keyword.GetCompletionItem(current);
                        if (item != null)
                        {
                            foreach (var value in item.Values)
                                list.Add(CreateCompletion(value, KnownMonikers.EnumerationItemPublic));
                        }
                    }
                    else if (span.ClassificationType.IsOfType(EditorConfigClassificationTypes.Severity))
                    {
                        if (span.Span.Contains(extent))
                            AddSeverity(list);
                    }
                }

                if (!list.Any())
                {
                    var item = Keyword.GetCompletionItem(current);

                    if (item != null)
                    {
                        var eq = line.GetText().IndexOf("=");

                        if (eq != -1)
                        {
                            var eqPos = eq + line.Start.Position;

                            if (triggerPoint.Value.Position > eqPos)
                                foreach (var value in item.Values)
                                    list.Add(CreateCompletion(value, KnownMonikers.EnumerationItemPublic));
                        }
                    }
                }
                else
                {
                    applicableTo = snapshot.CreateTrackingSpan(extent, SpanTrackingMode.EdgeInclusive);
                }
            }

            if (list.Any())
            {
                if (list.All(c => string.IsNullOrEmpty(c.IconAutomationText)))
                {
                    completionSets.Add(new FilteredCompletionSet(applicableTo, list, Enumerable.Empty<Completion4>(), null));
                }
                else
                {
                    var filters = new[] {
                        new IntellisenseFilter(KnownMonikers.Property, "Standard rules (Alt + S)", "s", "standard"),
                        new IntellisenseFilter(KnownMonikers.CSFileNode, ".NET analysis rules (Alt + C)", "c", "csharp"),
                        new IntellisenseFilter(KnownMonikers.DotNET, "C# analysis rules (Alt + D)", "d", "dotnet"),
                    };

                    completionSets.Add(new FilteredCompletionSet(applicableTo, list, Enumerable.Empty<Completion4>(), filters));
                }
            }
        }

        private void AddSeverity(List<Completion4> list)
        {
            list.Add(CreateCompletion("none", KnownMonikers.StatusSuppressed));
            list.Add(CreateCompletion("suggestion", KnownMonikers.StatusInformation));
            list.Add(CreateCompletion("warning", KnownMonikers.StatusWarning));
            list.Add(CreateCompletion("error", KnownMonikers.StatusError));
        }

        private Completion4 CreateCompletion(string name, ImageMoniker moniker, string tag = null, bool isSupported = true, string description = null)
        {
            string tooltip = description;
            IEnumerable<CompletionIcon2> icon = null;

            if (!isSupported)
            {
                icon = new[] { new CompletionIcon2(KnownMonikers.IntellisenseWarning, "warning", "") };
                tooltip = $"{Resources.Text.NotSupportedByVS}\r\n\r\n{description}";
            }

            return new Completion4(name, name, tooltip, moniker, tag, icon);
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