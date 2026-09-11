using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Text;

namespace EditorConfig
{
    class EditorConfigFormatter
    {
        private readonly EditorConfigDocument _document;
        private int _spaceBeforeEquals, _spaceAfterEquals;
        private int _spaceBeforeColon, _spaceAfterColon;

        public EditorConfigFormatter(ITextBuffer buffer)
        {
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public bool Format()
        {
            _spaceBeforeEquals = EditorConfigPackage.FormatterOptions.SpacesBeforeEquals;
            _spaceAfterEquals = EditorConfigPackage.FormatterOptions.SpacesAfterEquals;
            _spaceBeforeColon = EditorConfigPackage.FormatterOptions.SpacesBeforeColon;
            _spaceAfterColon = EditorConfigPackage.FormatterOptions.SpacesAfterColon;

            // Trim lines
            bool changed = TrimLines();

            // Format properties
            using (ITextEdit edit = _document.TextBuffer.CreateEdit())
            {
                int keywordLength = GetKeywordLength();

                FormatSection(edit, keywordLength);
                FormatRoot(edit, keywordLength);

                if (edit.HasEffectiveChanges)
                {
                    changed = true;
                    edit.Apply();
                }
            }

            return changed;
        }

        private bool TrimLines()
        {
            bool changed = false;

            using (ITextEdit edit = _document.TextBuffer.CreateEdit())
            {
                foreach (ITextSnapshotLine line in _document.TextBuffer.CurrentSnapshot.Lines)
                {
                    string originalText = line.GetText();
                    string newText = line.Extent.IsEmpty ? string.Empty : originalText.Trim();

                    if (originalText != newText)
                        edit.Replace(line.Start, line.Length, newText);
                }

                if (edit.HasEffectiveChanges)
                {
                    changed = true;
                    edit.Apply();
                }
            }

            return changed;
        }

        private int GetKeywordLength()
        {
            switch (EditorConfigPackage.FormatterOptions.FormattingType)
            {
                case FormattingType.Section:
                    return int.MinValue;
                case FormattingType.Document:
                    return _document.Sections.SelectMany(s => s.Properties).Select(p => p.Keyword.Text.Length).DefaultIfEmpty().Max();
                default:
                    return 0;
            }
        }

        private void FormatRoot(ITextEdit edit, int keywordLength)
        {
            if (_document.Root != null)
            {
                FormatProperty(_document.Root, Math.Max(keywordLength, 0), edit);
            }
        }

        private void FormatSection(ITextEdit edit, int keywordLength)
        {
            foreach (Section section in _document.Sections.Where(s => s.Properties.Any()).Reverse())
            {
                int length = keywordLength == int.MinValue ? section.Properties.Max(p => p.Keyword.Text.Length) : keywordLength;

                foreach (Property property in section.Properties.Where(p => p.IsValid).Reverse())
                {
                    FormatProperty(property, length, edit);
                }
            }
        }

        private void FormatProperty(Property property, int keywordLength, ITextEdit edit)
        {
            string originalText = edit.Snapshot.GetText(property.Span);
            string newText = FormatPropertyText(
                property,
                keywordLength,
                _spaceBeforeEquals,
                _spaceAfterEquals,
                _spaceBeforeColon,
                _spaceAfterColon);

            if (originalText != newText)
                edit.Replace(property.Span, newText);
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
