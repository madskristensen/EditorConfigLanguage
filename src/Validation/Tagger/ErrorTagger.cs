using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace EditorConfig
{
    class ErrorTagger : ITagger<IErrorTag>, IDisposable
    {
        private EditorConfigDocument _document;
        private IWpfTextView _view;
        private EditorConfigValidator _validator;
        private bool _hasLoaded;

        public ErrorTagger(IWpfTextView view)
        {
            _view = view;

            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            _validator = EditorConfigValidator.FromDocument(_document);
            _validator.Validated += DocumentValidated;

            ThreadHelper.JoinableTaskFactory.StartOnIdle(
                () =>
                {
                    _hasLoaded = true;
                    var span = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
                    return Task.CompletedTask;
                },
                VsTaskRunContext.UIThreadIdlePriority);
        }

        private void DocumentValidated(object sender, EventArgs e)
        {
            var span = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, 0, _view.TextBuffer.CurrentSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var tags = new List<ITagSpan<IErrorTag>>();

            if (_document.IsParsing || _validator.IsValidating || !_hasLoaded || !spans.Any() || spans[0].IsEmpty)
                return tags;

            ITextSnapshotLine line = spans[0].Start.GetContainingLine();
            IEnumerable<ParseItem> items = _document.ItemsInSpan(line.Extent);

            foreach (ParseItem item in items)
            {
                tags.AddRange(CreateError(item));
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
            switch (errorType)
            {
                case ErrorCategory.Error:
                    return PredefinedErrorTypeNames.SyntaxError;
                case ErrorCategory.Warning:
                    return PredefinedErrorTypeNames.Warning;
            }

            return ErrorFormatDefinition.Suggestion;
        }

        public void Dispose()
        {
            _validator.Validated -= DocumentValidated;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
