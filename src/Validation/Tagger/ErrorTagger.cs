using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace EditorConfig
{
    class ErrorTagger : ITagger<IErrorTag>, IDisposable
    {
        private readonly EditorConfigDocument _document;
        private readonly EditorConfigValidator _validator;

        public ErrorTagger(IWpfTextView view)
        {
            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            _validator = EditorConfigValidator.FromDocument(_document);
            _validator.Validated += DocumentValidated;
        }

        private void DocumentValidated(object sender, EventArgs e)
        {
            ITextSnapshot snapshot = _document.TextBuffer.CurrentSnapshot;
            var span = new SnapshotSpan(snapshot, 0, snapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var tags = new List<ITagSpan<IErrorTag>>();

            if (_validator.IsValidating || spans.Count == 0 || spans[0].IsEmpty ||
                !_validator.HasValidatedSnapshot(spans[0].Snapshot) ||
                !_document.TryGetParseResult(spans[0].Snapshot, out EditorConfigDocument.ParseResult result))
                return tags;

            // Get all items that have errors, regardless of span
            IEnumerable<ParseItem> itemsWithErrors = result.Items.Where(p => p.HasErrors);

            foreach (ParseItem item in itemsWithErrors)
            {
                // Check if this item's span intersects with any requested span
                foreach (SnapshotSpan requestedSpan in spans)
                {
                    if (requestedSpan.IntersectsWith(new Span(item.Span.Start, item.Span.Length)))
                    {
                        tags.AddRange(CreateError(item, result.Snapshot));
                        break; // Don't add same item multiple times
                    }
                }
            }

            return tags;
        }

        private IEnumerable<TagSpan<ErrorTag>> CreateError(ParseItem item, ITextSnapshot snapshot)
        {
            foreach (DisplayError error in item.Errors)
            {
                var span = new SnapshotSpan(snapshot, item.Span);
                string errorType = GetErrorType(error.Category);

                yield return new TagSpan<ErrorTag>(span, new ErrorTag(errorType, error.Name));
            }
        }

        public static string GetErrorType(ErrorCategory errorType)
        {
            return errorType switch
            {
                ErrorCategory.Error => PredefinedErrorTypeNames.SyntaxError,
                ErrorCategory.Warning => PredefinedErrorTypeNames.Warning,
                _ => ErrorFormatDefinition.Suggestion,
            };
        }

        public void Dispose()
        {
            _validator.Validated -= DocumentValidated;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
