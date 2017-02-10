using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EditorConfig
{
    class SuppressErrorAction : BaseSuggestedAction
    {
        private const string _suppressFormat = "# Suppress: {0}";
        private EditorConfigDocument _document;
        string _errorCode;

        public SuppressErrorAction(EditorConfigDocument document, string errorCode)
        {
            _document = document;
            _errorCode = errorCode;
        }

        public override string DisplayText
        {
            get { return $"Suppress {_errorCode}"; }
        }

        public override bool IsEnabled
        {
            get
            {
                return !_document.Suppressions.Contains(_errorCode, StringComparer.OrdinalIgnoreCase);
            }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            var range = new Span(0, 0);
            IEnumerable<string> errorCodes = _document.Suppressions.Union(new[] { _errorCode }).OrderBy(c => c);

            if (_document.Suppressions.Any())
            {
                int position = _document.ParseItems.First().Span.Start;
                ITextSnapshotLine line = _document.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
                range = Span.FromBounds(line.Start, line.EndIncludingLineBreak);
            }

            string text = string.Format(_suppressFormat, string.Join(" ", errorCodes)) + Environment.NewLine;

            using (ITextEdit edit = _document.TextBuffer.CreateEdit())
            {
                edit.Replace(range, text);
                edit.Apply();
            }
        }
    }
}
