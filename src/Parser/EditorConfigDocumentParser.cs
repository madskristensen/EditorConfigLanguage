using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private static Regex _property = new Regex(@"^\s*(?<keyword>[^;\[#:\s=]+)\s*[=:]?\s*(?<value>[^;#:]+)?(\s*:\s*(?<severity>[^;#:\s]+))?");
        private static Regex _section = new Regex(@"^\s*(?<section>\[.+)");
        private static Regex _comment = new Regex(@"^\s*[#;].+");
        private static Regex _unknown = new Regex(@"\s*(?<unknown>.+)");
        private static Regex _suppress = new Regex(@"^(?<comment>#\s*suppress\s*):?\s*(?<errors>(\w{0,5}\s*)+)$", RegexOptions.IgnoreCase);

        /// <summary>Returns true if the document is currently being parsed.</summary>
        public bool IsParsing { get; private set; }

        private void InitializeParser()
        {
            System.Threading.Tasks.Task task = ParseAsync();
            TextBuffer.Changed += BufferChangedAsync;
        }

        private async void BufferChangedAsync(object sender, TextContentChangedEventArgs e)
        {
            await ParseAsync();
        }

        private System.Threading.Tasks.Task ParseAsync()
        {
            IsParsing = true;

            return System.Threading.Tasks.Task.Run(() =>
            {
                var items = new List<ParseItem>();
                var sections = new List<Section>();
                var properties = new List<Property>();

                Suppressions = new List<string>();
                Section parentSection = null;

                foreach (ITextSnapshotLine line in TextBuffer.CurrentSnapshot.Lines)
                {
                    string text = line.GetText();

                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    // Suppression
                    if (IsMatch(_suppress, text, out var match))
                    {
                        ParseItem comment = CreateParseItem(ItemType.Comment, line, match.Groups["comment"]);
                        AddToList(items, comment);

                        Group errorsGroup = match.Groups["errors"];
                        string value = errorsGroup.Value;

                        var c = new Regex(@"\w+");
                        foreach (Match code in c.Matches(match.Value, errorsGroup.Index))
                        {
                            ParseItem errors = CreateParseItem(ItemType.Suppression, line, code);
                            AddToList(items, errors);

                            if (!Suppressions.Contains(code.Value) && ErrorCatalog.All.Any(ec => ec.Code == code.Value))
                                Suppressions.Add(code.Value);
                        }
                    }
                    // Comment
                    else if (IsMatch(_comment, text, out match))
                    {
                        ParseItem comment = CreateParseItem(ItemType.Comment, line, match);
                        AddToList(items, comment);
                    }
                    // Section
                    else if (IsMatch(_section, text, out match))
                    {
                        ParseItem section = CreateParseItem(ItemType.Section, line, match.Groups["section"]);
                        AddToList(items, section);

                        var s = new Section(section);
                        sections.Add(s);
                        parentSection = s;
                    }
                    // Property
                    else if (IsMatch(_property, text, out match))
                    {
                        ParseItem keyword = CreateParseItem(ItemType.Keyword, line, match.Groups["keyword"]);
                        AddToList(items, keyword);

                        var property = new Property(keyword);

                        if (parentSection == null)
                            properties.Add(property);
                        else
                            parentSection.Properties.Add(property);

                        if (match.Groups["value"].Success)
                        {
                            ParseItem value = CreateParseItem(ItemType.Value, line, match.Groups["value"]);
                            AddToList(items, value);
                            property.Value = value;
                        }

                        if (match.Groups["severity"].Success)
                        {
                            ParseItem severity = CreateParseItem(ItemType.Severity, line, match.Groups["severity"]);
                            AddToList(items, severity);
                            property.Severity = severity;
                        }
                    }

                    if (match.Success && match.Length < text.Length)
                    {
                        string remaining = text.Substring(match.Length);

                        if (!string.IsNullOrEmpty(remaining) && IsMatch(_unknown, remaining, out Match unknownMatch))
                        {
                            Group group = unknownMatch.Groups["unknown"];
                            if (!string.IsNullOrWhiteSpace(group.Value))
                            {
                                var span = new Span(line.Start + match.Length + group.Index, group.Length);
                                var unknown = new ParseItem(this, ItemType.Unknown, span, remaining);
                                AddToList(items, unknown);
                            }
                        }
                    }
                }

                ParseItems = items;
                Sections = sections;
                Properties = properties;

                IsParsing = false;

                Parsed?.Invoke(this, EventArgs.Empty);
            });
        }

        private void AddToList(List<ParseItem> items, ParseItem item)
        {
            if (item.Span.Length == 0)
                return;

            items.Add(item);
        }

        private static bool IsMatch(Regex regex, string input, out Match match)
        {
            match = regex.Match(input);
            return match.Success;
        }

        private ParseItem CreateParseItem(ItemType type, ITextSnapshotLine line, Capture match)
        {
            string trimmed = match.Value.TrimEnd();
            var matchSpan = new SnapshotSpan(line.Snapshot, line.Start.Position + match.Index, trimmed.Length);

            var item = new ParseItem(this, type, matchSpan, trimmed);

            return item;
        }

        private void DisposeParser()
        {
            Parsed = null;
        }

        /// <summary>The event is fired when the document has been parsed.</summary>
        public event EventHandler Parsed;
    }
}
