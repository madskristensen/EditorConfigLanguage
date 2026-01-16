using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    class EditorConfigCompletionSource(ITextBuffer buffer) : ICompletionSource
    {
        private readonly EditorConfigDocument _document = EditorConfigDocument.FromTextBuffer(buffer);
        private bool _disposed;

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed || _document.IsParsing)
                return;

            ITextSnapshot snapshot = buffer.CurrentSnapshot;
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
            string lineText = line.GetText();

            // Determine what type of completion to show
            bool isTypingNewKeyword = !string.IsNullOrWhiteSpace(lineText) &&
                                      !lineText.Contains("=") &&
                                      !lineText.TrimStart().StartsWith("[") &&
                                      !lineText.TrimStart().StartsWith("#") &&
                                      !lineText.TrimStart().StartsWith(";");

            bool isInSeverityPosition = IsInSeverityPosition(lineText, position - line.Start.Position);

            // Property/Keyword completion
            if (string.IsNullOrWhiteSpace(lineText) || parseItem?.ItemType == ItemType.Keyword || isTypingNewKeyword)
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

                    // Use HashSet for O(1) lookup instead of List.Contains which is O(n)
                    HashSet<string> usedKeywords;

                    ParseItem parseItemSection = _document.ParseItems.LastOrDefault(p => p.ItemType == ItemType.Section && p.Span.Start < position);
                    if (parseItemSection != null)
                    {
                        Section section = _document.Sections.FirstOrDefault(x => x.Item.Text.Equals(parseItemSection.Text, StringComparison.OrdinalIgnoreCase));
                        if (section != null)
                        {
                            usedKeywords = new HashSet<string>(section.Properties.Select(x => x.Keyword.Text), StringComparer.OrdinalIgnoreCase);
                        }
                        else
                        {
                            usedKeywords = [.. _document.GetAllIncludedRules()];
                        }
                    }
                    else
                    {
                        usedKeywords = [.. _document.GetAllIncludedRules()];
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
            // Severity completion - check BEFORE value completion
            else if (isInSeverityPosition || parseItem?.ItemType == ItemType.Severity)
            {
                Keyword keyword = GetKeywordForLine(line);
                if (keyword != null && keyword.RequiresSeverity)
                {
                    AddSeverity(list);
                    moniker = "severity";
                }
            }
            // Value completion
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
                        var usedValues = new HashSet<string>(
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
        /// Checks if the position is in a severity position (after the : that follows a value).
        /// </summary>
        private static bool IsInSeverityPosition(string lineText, int posInLine)
        {
            int eq = lineText.IndexOf('=');
            if (eq < 0 || posInLine <= eq)
                return false;

            // Find the last : in the value portion (after =)
            string afterEq = lineText.Substring(eq + 1);
            int colonIndex = afterEq.LastIndexOf(':');

            if (colonIndex < 0)
                return false;

            // The colon position in the line
            int colonPosInLine = eq + 1 + colonIndex;

            // We're in severity position if cursor is after the colon
            return posInLine > colonPosInLine;
        }

        /// <summary>
        /// Checks if the position is in a value position (after the = sign but not in severity position).
        /// </summary>
        private static bool IsInValuePosition(SnapshotSpan line, int position)
        {
            string lineText = line.GetText();
            int eq = lineText.IndexOf('=');
            if (eq < 0)
                return false;

            int eqPos = eq + line.Start.Position;
            int posInLine = position - line.Start.Position;

            // Not in value position if we're in severity position
            if (IsInSeverityPosition(lineText, posInLine))
                return false;

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
        /// </summary>
        private static ITrackingSpan GetApplicableSpan(ITextSnapshot snapshot, SnapshotSpan line, int position)
        {
            string lineText = line.GetText();
            int posInLine = position - line.Start.Position;

            bool isKeywordPosition = !lineText.Contains("=");
            bool isSeverityPosition = IsInSeverityPosition(lineText, posInLine);

            int spanStart;
            int spanEnd;

            if (isKeywordPosition)
            {
                // For keywords: find start of word (skip leading whitespace)
                spanStart = 0;
                while (spanStart < lineText.Length && char.IsWhiteSpace(lineText[spanStart]))
                    spanStart++;

                // End is at cursor position or end of current word
                spanEnd = posInLine;
                for (int i = posInLine; i < lineText.Length; i++)
                {
                    if (char.IsWhiteSpace(lineText[i]))
                        break;
                    spanEnd = i + 1;
                }
            }
            else if (isSeverityPosition)
            {
                // For severity: find start after the last :
                int eq = lineText.IndexOf('=');
                string afterEq = lineText.Substring(eq + 1);
                int colonIndex = afterEq.LastIndexOf(':');
                spanStart = eq + 1 + colonIndex + 1;

                // Skip whitespace after :
                while (spanStart < lineText.Length && char.IsWhiteSpace(lineText[spanStart]))
                    spanStart++;

                // End is at end of line or before comment
                spanEnd = lineText.Length;
                for (int i = spanStart; i < lineText.Length; i++)
                {
                    if (lineText[i] == '#' || lineText[i] == ';')
                    {
                        spanEnd = i;
                        break;
                    }
                }

                // Trim trailing whitespace
                while (spanEnd > spanStart && char.IsWhiteSpace(lineText[spanEnd - 1]))
                    spanEnd--;
            }
            else
            {
                // For values: find the start of the current value (after = or ,)
                spanStart = posInLine;
                for (int i = posInLine - 1; i >= 0; i--)
                {
                    char c = lineText[i];
                    if (c == '=' || c == ',')
                    {
                        spanStart = i + 1;
                        while (spanStart < lineText.Length && char.IsWhiteSpace(lineText[spanStart]))
                            spanStart++;
                        break;
                    }
                }

                // Find the end of the current value (before , or : or end of line)
                spanEnd = posInLine;
                for (int i = posInLine; i < lineText.Length; i++)
                {
                    char c = lineText[i];
                    if (c == ',' || c == ':' || c == ';' || c == '#')
                    {
                        spanEnd = i;
                        break;
                    }
                    spanEnd = i + 1;
                }

                // Trim trailing whitespace
                while (spanEnd > spanStart && char.IsWhiteSpace(lineText[spanEnd - 1]))
                    spanEnd--;
            }

            int absoluteStart = line.Start.Position + spanStart;
            int spanLength = spanEnd - spanStart;

            if (absoluteStart > position)
                absoluteStart = position;

            if (spanLength < 0)
                spanLength = 0;

            return snapshot.CreateTrackingSpan(absoluteStart, spanLength, SpanTrackingMode.EdgeInclusive);
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
            if (int.TryParse(item.Name, out _))
                text = "<integer>";

            if (!item.IsSupported)
            {
                icon = [new CompletionIcon2(KnownMonikers.IntellisenseWarning, "warning", "")];
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
