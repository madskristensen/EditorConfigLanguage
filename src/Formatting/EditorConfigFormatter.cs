using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EditorConfig
{
    class EditorConfigFormatter
    {
        private readonly EditorConfigDocument _document;

        public EditorConfigFormatter(ITextBuffer buffer)
        {
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public bool Format()
        {
            FormatterOptions options = EditorConfigPackage.FormatterOptions;
            return Format(options.FormattingType, options.SpacesBeforeEquals, options.SpacesAfterEquals, options.SpacesBeforeColon, options.SpacesAfterColon);
        }

        internal bool Format(FormattingType formattingType, int spaceBeforeEquals, int spaceAfterEquals, int spaceBeforeColon, int spaceAfterColon)
        {
            using ITextEdit edit = _document.TextBuffer.CreateEdit();
            EditorConfigDocument.ParseResult result = _document.ParseSnapshot(edit.Snapshot, CancellationToken.None);
            Dictionary<int, string> replacements = GetFormattedLines(result, formattingType,
                spaceBeforeEquals, spaceAfterEquals, spaceBeforeColon, spaceAfterColon);

            foreach (ITextSnapshotLine line in edit.Snapshot.Lines)
            {
                string originalText = line.GetText();
                string newText = replacements.TryGetValue(line.Start.Position, out string formatted) ? formatted : originalText.Trim();
                if (originalText != newText)
                    edit.Replace(line.Start, line.Length, newText);
            }

            if (!edit.HasEffectiveChanges)
                return false;

            edit.Apply();
            return true;
        }

        internal static Dictionary<int, string> GetFormattedLines(EditorConfigDocument.ParseResult result, FormattingType formattingType,
            int spaceBeforeEquals, int spaceAfterEquals, int spaceBeforeColon, int spaceAfterColon)
        {
            var replacements = new Dictionary<int, string>();
            int documentWidth = formattingType == FormattingType.Document
                ? result.Sections.SelectMany(section => section.Properties).Select(property => property.Keyword.Text.Length).DefaultIfEmpty().Max()
                : 0;

            foreach (Section section in result.Sections)
            {
                int width = formattingType == FormattingType.Section
                    ? section.Properties.Select(property => property.Keyword.Text.Length).DefaultIfEmpty().Max()
                    : documentWidth;
                foreach (Property property in section.Properties.Where(property => property.IsValid))
                    AddProperty(property, width);
            }

            Property root = result.Properties.FirstOrDefault(property => property.Keyword.Text.Is(SchemaCatalog.Root));
            if (root != null)
                AddProperty(root, documentWidth);

            return replacements;

            void AddProperty(Property property, int width)
            {
                ITextSnapshotLine line = result.Snapshot.GetLineFromPosition(property.Keyword.Span.Start);
                string suffix = line.GetText().Substring(property.Span.End - line.Start.Position).TrimEnd();
                replacements[line.Start.Position] = FormatPropertyText(property, width, spaceBeforeEquals, spaceAfterEquals, spaceBeforeColon, spaceAfterColon) + suffix;
            }
        }

        internal static string FormatPropertyText(
            Property property,
            int keywordLength,
            int spaceBeforeEquals,
            int spaceAfterEquals,
            int spaceBeforeColon,
            int spaceAfterColon)
        {
            string newText = property.Keyword.Text.PadRight(keywordLength);

            if (property.Value != null)
                newText += $"{new string(' ', spaceBeforeEquals)}={new string(' ', spaceAfterEquals)}{property.Value.Text}";

            if (property.Severity != null)
                newText += $"{new string(' ', spaceBeforeColon)}:{new string(' ', spaceAfterColon)}{property.Severity.Text}";

            return newText;
        }
    }
}
