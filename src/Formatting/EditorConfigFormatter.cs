using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Text;

namespace EditorConfig
{
    class EditorConfigFormatter
    {
        private EditorConfigDocument _document;
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
                    return _document.Sections.SelectMany(s => s.Properties).Max(p => p.Keyword.Text.Length);
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
            string newText = property.Keyword.Text.PadRight(keywordLength);

            string spaceBeforeEquals = string.Empty.PadRight(_spaceBeforeEquals);
            string spaceAfterEquals = string.Empty.PadRight(_spaceAfterEquals);
            string spaceBeforeColon = string.Empty.PadRight(_spaceBeforeColon);
            string spaceAfterColon = string.Empty.PadRight(_spaceAfterColon);

            if (property.Value != null)
                newText += $"{spaceBeforeEquals}={spaceAfterEquals}{property.Value.Text}";

            if (property.Severity != null)
                newText += $"{spaceBeforeColon}:{spaceAfterColon}{property.Severity.Text}";

            if (originalText != newText)
                edit.Replace(property.Span, newText);
        }
    }
}
