using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace EditorConfig
{
    class EditorConfigCompletionSource : ICompletionSource
    {
        private readonly ITextBuffer _buffer;
        private readonly EditorConfigDocument _document;
        private bool _disposed;

        public EditorConfigCompletionSource(ITextBuffer buffer, ITextStructureNavigatorSelectorService navigator)
        {
            _buffer = buffer;
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed || _document.IsParsing)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(snapshot);

            if (triggerPoint == null || !triggerPoint.HasValue)
                return;

            SnapshotSpan line = triggerPoint.Value.GetContainingLine().Extent;
            var list = new List<Completion4>();
            int position = triggerPoint.Value.Position;
            ITrackingSpan applicableTo = snapshot.CreateTrackingSpan(position, 0, SpanTrackingMode.EdgeInclusive);

            ParseItem prev = _document.ParseItems.LastOrDefault(p => p.Span.Start < position && !p.Span.Contains(position - 1));
            ParseItem parseItem = _document.ItemAtPosition(position);
            string moniker = null;

            // Property/Keyword completion
            if (string.IsNullOrWhiteSpace(line.GetText()) || parseItem?.ItemType == ItemType.Keyword)
            {
                bool isInRoot = !_document.ParseItems.Exists(p => p.ItemType == ItemType.Section && p.Span.Start < position);

                if (isInRoot)
                {
                    if (SchemaCatalog.TryGetKeyword(SchemaCatalog.Root, out Keyword root))
                        list.Add(CreateCompletion(root, root.Category));
                }
                else
                {
                    IEnumerable<Keyword> properties = EditorConfigPackage.CompletionOptions.ShowHiddenKeywords ? SchemaCatalog.AllKeywords : SchemaCatalog.VisibleKeywords;
                    IEnumerable<Keyword> items = properties.Where(i => i.Name != SchemaCatalog.Root);
                    IEnumerable<string> usedKeywords = _document.GetAllIncludedRules();

                    ParseItem parseItemSection = _document.ParseItems.LastOrDefault(p => p.ItemType == ItemType.Section && p.Span.Start < position);
                    if (parseItemSection != null)
                    {
                        Section section = _document.Sections.FirstOrDefault(x => x.Item.Text.Equals(parseItemSection.Text, StringComparison.OrdinalIgnoreCase));
                        if (section != null)
                        {
                            usedKeywords = section.Properties.Select(x => x.Keyword.Text).ToList();
                        }
                    }

                    foreach (Keyword property in items)
                    {
                        string keyword = property.Name;

                        if (usedKeywords.Contains(keyword) && !keyword.StartsWith("dotnet_naming_", StringComparison.OrdinalIgnoreCase))
                            continue;

                        list.Add(CreateCompletion(property, property.Category));
                    }
                }

                moniker = "keyword";
            }
            // Value completion - check if we're in a value position on the line
            else if (parseItem?.ItemType == ItemType.Value || IsInValuePosition(line, position))
            {
                Keyword keyword = GetKeywordForLine(line);

                if (keyword != null)
                {
                    string valueText = GetValueTextFromLine(line);

                    if (!keyword.SupportsMultipleValues && valueText.Contains(","))
                        return;

                    if (keyword.SupportsMultipleValues)
                    {
                        HashSet<string> usedValues = new HashSet<string>(
                            valueText.Split([','], StringSplitOptions.RemoveEmptyEntries)
                                .Select(v => v.Trim()),
                            StringComparer.OrdinalIgnoreCase);

                        foreach (Value value in keyword.Values)
                        {
                            if (!usedValues.Contains(value.Name))
                                list.Add(CreateCompletion(value, iconAutomation: "value"));
                        }
                    }
                    else
                    {
                        foreach (Value value in keyword.Values)
                            list.Add(CreateCompletion(value, iconAutomation: "value"));
                    }
                }

                moniker = "value";
            }
            // Severity completion
            else if ((position > 0 && snapshot.Length > 1 && snapshot.GetText(position - 1, 1) == ":") || parseItem?.ItemType == ItemType.Severity)
            {
                if (prev?.ItemType == ItemType.Value || parseItem?.ItemType == ItemType.Severity)
                {
                    Property prop = _document.PropertyAtPosition(prev.Span.Start);
                    if (SchemaCatalog.TryGetKeyword(prop?.Keyword?.Text, out Keyword key) && key.RequiresSeverity)
                        AddSeverity(list);

                    moniker = "severity";
                }
            }
            // Suppression completion
            else if (parseItem?.ItemType == ItemType.Suppression)
            {
                foreach (Error code in ErrorCatalog.All.OrderBy(e => e.Code))
                    list.Add(CreateCompletion(code));

                moniker = "suppression";
            }

            // Calculate the applicable span for completion
            if (list.Any())
            {
                applicableTo = GetApplicableSpan(snapshot, line, position);
            }

            CreateCompletionSet(moniker, completionSets, list, applicableTo);
        }

        /// <summary>
        /// Checks if the position is in a value position (after the = sign) on the line.
        /// </summary>
        private static bool IsInValuePosition(SnapshotSpan line, int position)
        {
            string lineText = line.GetText();
            int eq = lineText.IndexOf('=');
            if (eq < 0)
                return false;

            int eqPos = eq + line.Start.Position;
            return position > eqPos;
        }

        /// <summary>
        /// Gets the keyword for the given line by parsing the keyword text before the = sign.
        /// </summary>
        private static Keyword GetKeywordForLine(SnapshotSpan line)
        {
            string lineText = line.GetText();
            int eq = lineText.IndexOf('=');
            if (eq < 0)
                return null;

            string keywordText = lineText.Substring(0, eq).Trim();
            SchemaCatalog.TryGetKeyword(keywordText, out Keyword keyword);
            return keyword;
        }

        /// <summary>
        /// Gets the value text from the line (everything after = and before any severity suffix).
        /// </summary>
        private static string GetValueTextFromLine(SnapshotSpan line)
        {
            string lineText = line.GetText();
            int eq = lineText.IndexOf('=');
            if (eq < 0)
                return "";

            string valueText = lineText.Substring(eq + 1).TrimEnd();

            // Check for severity suffix and remove it
            int colonIndex = valueText.LastIndexOf(':');
            if (colonIndex > 0 && !valueText.Substring(colonIndex + 1).Trim().Contains(","))
            {
                valueText = valueText.Substring(0, colonIndex);
            }

            return valueText;
        }

        /// <summary>
        /// Gets the applicable span for completion - the text that will be replaced when a completion is selected.
        /// For multi-value properties, this is just the current value segment, not the entire value list.
        /// </summary>
        private static ITrackingSpan GetApplicableSpan(ITextSnapshot snapshot, SnapshotSpan line, int position)
        {
            string lineText = line.GetText();
            int posInLine = position - line.Start.Position;

            // Find the start of the current value (after = or ,)
            int valueStart = posInLine;
            for (int i = posInLine - 1; i >= 0; i--)
            {
                char c = lineText[i];
                if (c == '=' || c == ',')
                {
                    valueStart = i + 1;
                    // Skip whitespace after delimiter
                    while (valueStart < lineText.Length && char.IsWhiteSpace(lineText[valueStart]))
                        valueStart++;
                    break;
                }
            }

            // Find the end of the current value (before , or : or end of line)
            int valueEnd = posInLine;
            for (int i = posInLine; i < lineText.Length; i++)
            {
                char c = lineText[i];
                if (c == ',' || c == ':' || c == ';' || c == '#')
                {
                    valueEnd = i;
                    break;
                }
                valueEnd = i + 1;
            }

            // Trim trailing whitespace
            while (valueEnd > valueStart && char.IsWhiteSpace(lineText[valueEnd - 1]))
                valueEnd--;

            int spanStart = line.Start.Position + valueStart;
            int spanLength = valueEnd - valueStart;

            if (spanStart > position)
                spanStart = position;

            if (spanLength < 0)
                spanLength = 0;

            return snapshot.CreateTrackingSpan(spanStart, spanLength, SpanTrackingMode.EdgeInclusive);
        }

        private static void CreateCompletionSet(string moniker, IList<CompletionSet> completionSets, List<Completion4> list, ITrackingSpan applicableTo)
        {
            if (list.Any())
            {
                if (moniker == "keyword")
                {
                    IntellisenseFilter[] filters = [
                        new IntellisenseFilter(KnownMonikers.Property, "Standard rules (Alt + S)", "s", Category.Standard.ToString()),
                        new IntellisenseFilter(KnownMonikers.CPPFileNode, "C++ rules (Alt + P)", "p", Category.CPP.ToString()),
                        new IntellisenseFilter(KnownMonikers.CSFileNode, "C# analysis rules (Alt + C)", "c", Category.CSharp.ToString()),
                        new IntellisenseFilter(KnownMonikers.DotNET, ".NET analysis rules (Alt + D)", "d", Category.DotNet.ToString()),
                        new IntellisenseFilter(KnownMonikers.VBFileNode, "VB.NET analysis rules (Alt + V)", "v", Category.VisualBasic.ToString()),
                    ];

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
            foreach (Severity severity in SchemaCatalog.Severities)
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

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
