using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Text;

namespace EditorConfig
{
    class EditorConfigFormatter
    {
        private EditorConfigDocument _document;

        public EditorConfigFormatter(ITextBuffer buffer)
        {
            _document = EditorConfigDocument.FromTextBuffer(buffer);
        }

        public void Format()
        {
            // Trim lines
            TrimLines();

            // Format properties
            using (var edit = _document.TextBuffer.CreateEdit())
            {
                int keywordLength = GetKeywordLength();

                FormatSection(edit, keywordLength);
                FormatRoot(edit, keywordLength);

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }
        }

        private void TrimLines()
        {
            var sb = new StringBuilder();

            foreach (var line in _document.TextBuffer.CurrentSnapshot.Lines)
            {
                if (line.Extent.IsEmpty)
                    sb.AppendLine();
                else
                    sb.AppendLine(line.GetText().Trim());
            }

            using (var edit = _document.TextBuffer.CreateEdit())
            {
                edit.Replace(0, edit.Snapshot.Length, sb.ToString());
                edit.Apply();
            }
        }

        private int GetKeywordLength()
        {
            switch (EditorConfigPackage.Options.FormattingType)
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
            foreach (var section in _document.Sections.Reverse<Section>())
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

            if (property.Value != null)
                newText += $" = {property.Value.Text}";

            if (property.Severity != null)
                newText += $":{property.Severity.Text}";

            if (originalText != newText)
                edit.Replace(property.Span, newText);
        }
    }
}
