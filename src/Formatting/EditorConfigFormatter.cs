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
            var changed = TrimLines();

            // Format properties
            using (var edit = _document.TextBuffer.CreateEdit())
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

            using (var edit = _document.TextBuffer.CreateEdit())
            {
                foreach (var line in _document.TextBuffer.CurrentSnapshot.Lines)
                {
                    var originalText = line.GetText();
                    var newText = line.Extent.IsEmpty ? string.Empty : originalText.Trim();

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
            foreach (var section in _document.Sections.Where(s => s.Properties.Any()).Reverse())
            {
                var length = keywordLength == int.MinValue ? section.Properties.Max(p => p.Keyword.Text.Length) : keywordLength;

                foreach (var property in section.Properties.Where(p => p.IsValid).Reverse())
                {
                    FormatProperty(property, length, edit);
                }
            }
        }

        private void FormatProperty(Property property, int keywordLength, ITextEdit edit)
        {
            var originalText = edit.Snapshot.GetText(property.Span);
            var newText = property.Keyword.Text.PadRight(keywordLength);

            var spaceBeforeEquals = string.Empty.PadRight(_spaceBeforeEquals);
            var spaceAfterEquals = string.Empty.PadRight(_spaceAfterEquals);
            var spaceBeforeColon = string.Empty.PadRight(_spaceBeforeColon);
            var spaceAfterColon = string.Empty.PadRight(_spaceAfterColon);

            if (property.Value != null)
                newText += $"{spaceBeforeEquals}={spaceAfterEquals}{property.Value.Text}";

            if (property.Severity != null)
                newText += $"{spaceBeforeColon}:{spaceAfterColon}{property.Severity.Text}";

            if (originalText != newText)
                edit.Replace(property.Span, newText);
        }
    }
}
