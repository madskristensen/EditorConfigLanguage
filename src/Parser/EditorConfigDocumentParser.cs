using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private const int _parseDelay = 150;
        private static readonly Regex _property = new(@"^\s*(?<keyword>[^;\[#:\s=]+)\s*=\s*(?<value>.*?)(?:\s*:\s*(?<severity>none|silent|suggestion|warning|error|default|refactoring))?\s*(?<comment>[#;].*)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _section = new(@"^\s*(?<section>\[.+)", RegexOptions.Compiled);
        private static readonly Regex _comment = new(@"^\s*[#;].*", RegexOptions.Compiled);
        private static readonly Regex _unknown = new(@"\s*(?<unknown>.+)", RegexOptions.Compiled);
        private static readonly Regex _suppress = new(@"^(?<comment>#\s*suppress\s*):?\s*(?<errors>[\w\s]*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex _suppressionCode = new(@"\w+", RegexOptions.Compiled);
        private int _latestParseRequestId;
        private CancellationTokenSource _parseCancellation;
        private Task _parsingTask = Task.CompletedTask;
        private bool _parserDisposed;

        /// <summary>Returns true if the document is currently being parsed.</summary>
        public bool IsParsing { get; private set; }

        private void InitializeParser()
        {
            TextBuffer.Changed += BufferChanged;
            RequestParse(TextBuffer.CurrentSnapshot, TimeSpan.Zero);
        }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            RequestParse(e.After, TimeSpan.FromMilliseconds(_parseDelay));
        }

        private void RequestParse(ITextSnapshot snapshot, TimeSpan delay)
        {
            if (_parserDisposed)
                return;

            int parseRequestId = Interlocked.Increment(ref _latestParseRequestId);
            var cancellation = new CancellationTokenSource();
            CancellationTokenSource previous = Interlocked.Exchange(ref _parseCancellation, cancellation);
            previous?.Cancel();
            previous?.Dispose();

            IsParsing = true;
            _parsingTask = ParseAsync(snapshot, parseRequestId, delay, cancellation.Token);
        }

        private async Task ParseAsync(ITextSnapshot snapshot, int parseRequestId, TimeSpan delay, CancellationToken cancellationToken)
        {
            try
            {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                ParseResult result = await Task.Run(() => ParseSnapshot(snapshot, cancellationToken), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (parseRequestId != Volatile.Read(ref _latestParseRequestId))
                    return;

                Suppressions = result.Suppressions;
                ParseItems = result.Items;
                Sections = result.Sections;
                Properties = result.Properties;
                NamingEntities = result.NamingEntities;

                Parsed?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Telemetry.TrackException("Parse", ex);
            }
            finally
            {
                if (parseRequestId == Volatile.Read(ref _latestParseRequestId))
                {
                    IsParsing = false;
                }
            }
        }

        private ParseResult ParseSnapshot(ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            var items = new List<ParseItem>();
            var sections = new List<Section>();
            var properties = new List<Property>();
            var suppressions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Section parentSection = null;

            foreach (ITextSnapshotLine line in snapshot.Lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string text = line.GetText();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                // Suppression
                if (IsMatch(_suppress, text, out Match match))
                {
                    ParseItem comment = CreateParseItem(ItemType.Comment, line, match.Groups["comment"]);
                    AddToList(items, comment);

                    Group errorsGroup = match.Groups["errors"];

                    foreach (Match code in _suppressionCode.Matches(match.Value, errorsGroup.Index))
                    {
                        ParseItem errors = CreateParseItem(ItemType.Suppression, line, code);
                        AddToList(items, errors);

                        if (ErrorCatalog.TryGetErrorCode(code.Value, out _))
                            suppressions.Add(code.Value);
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
                else if (TryMatchProperty(text, out match))
                {
                    ParseItem keyword = CreateParseItem(ItemType.Keyword, line, match.Groups["keyword"]);
                    AddToList(items, keyword);

                    var property = new Property(keyword);

                    if (parentSection == null)
                        properties.Add(property);
                    else
                        parentSection.Properties.Add(property);

                    if (match.Groups["value"].Success && !string.IsNullOrWhiteSpace(match.Groups["value"].Value))
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

                    if (match.Groups["comment"].Success)
                    {
                        ParseItem comment = CreateParseItem(ItemType.Comment, line, match.Groups["comment"]);
                        AddToList(items, comment);
                    }
                }
                else
                {
                    ParseItem unknown = CreateParseItem(ItemType.Unknown, line, _unknown.Match(text).Groups["unknown"]);
                    AddToList(items, unknown);
                }
            }

            foreach (Section section in sections)
                section.NamingEntities = NamingEntityIndex.Create(section.Properties);

            NamingEntityIndex namingEntities = NamingEntityIndex.Create(sections.SelectMany(section => section.Properties));
            return new ParseResult(items, sections, properties, suppressions, namingEntities);
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

        internal static bool TryMatchProperty(string input, out Match match)
        {
            return IsMatch(_property, input, out match);
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
            _parserDisposed = true;
            TextBuffer.Changed -= BufferChanged;
            Interlocked.Increment(ref _latestParseRequestId);
            CancellationTokenSource cancellation = Interlocked.Exchange(ref _parseCancellation, null);
            cancellation?.Cancel();
            cancellation?.Dispose();
            Parsed = null;
        }

        internal Task ParsingTask => _parsingTask;

        private sealed class ParseResult(
            List<ParseItem> items,
            List<Section> sections,
            List<Property> properties,
            HashSet<string> suppressions,
            NamingEntityIndex namingEntities)
        {
            public List<ParseItem> Items { get; } = items;
            public List<Section> Sections { get; } = sections;
            public List<Property> Properties { get; } = properties;
            public HashSet<string> Suppressions { get; } = suppressions;
            public NamingEntityIndex NamingEntities { get; } = namingEntities;
        }

        /// <summary>The event is fired when the document has been parsed.</summary>
        public event EventHandler Parsed;
    }
}
