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
        private readonly IWpfTextView _view;
        private readonly EditorConfigValidator _validator;

        public ErrorTagger(IWpfTextView view)
        {
            _view = view;

            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            _validator = EditorConfigValidator.FromDocument(_document);
            _validator.Validated += DocumentValidated;
        }

        private void DocumentValidated(object sender, EventArgs e)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var span = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, 0, _view.TextBuffer.CurrentSnapshot.Length);
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            });
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var tags = new List<ITagSpan<IErrorTag>>();

            if (_document.IsParsing || _validator.IsValidating || spans.Count == 0 || spans[0].IsEmpty)
                return tags;

            // Get all items that have errors, regardless of span
            IEnumerable<ParseItem> itemsWithErrors = _document.ParseItems?.Where(p => p.HasErrors) ?? [];

            foreach (ParseItem item in itemsWithErrors)
            {
                // Check if this item's span intersects with any requested span
                foreach (SnapshotSpan requestedSpan in spans)
                {
                    if (requestedSpan.IntersectsWith(new Span(item.Span.Start, item.Span.Length)))
                    {
                        tags.AddRange(CreateError(item));
                        break; // Don't add same item multiple times
                    }
                }
            }

            return tags;
        }

        private IEnumerable<TagSpan<ErrorTag>> CreateError(ParseItem item)
        {
            foreach (DisplayError error in item.Errors)
            {
                var span = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, item.Span);
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
