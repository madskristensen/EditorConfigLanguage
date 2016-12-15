using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace EditorConfig
{
    class ErrorTagger : ITagger<IErrorTag>, IDisposable
    {
        private ErrorListProvider _errorlist;
        private string _file;
        private EditorConfigDocument _document;
        private IWpfTextView _view;
        private EditorConfigValidator _validator;
        private bool _hasLoaded;

        public ErrorTagger(IWpfTextView view, ErrorListProvider errorlist, string file)
        {
            _view = view;
            _errorlist = errorlist;
            _file = file;

            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            _validator = EditorConfigValidator.FromDocument(_document);
            _validator.Validated += DocumentValidated;

            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                _hasLoaded = true;
                var span = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            });
        }

        private void DocumentValidated(object sender, EventArgs e)
        {
            var span = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, 0, _view.TextBuffer.CurrentSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var tags = new List<ITagSpan<IErrorTag>>();

            if (_document.IsParsing || !_hasLoaded || !spans.Any() || spans[0].IsEmpty)
                return tags;

            var line = spans[0].Start.GetContainingLine();
            var items = _document.ItemsInSpan(line.Extent);

            try
            {
                _errorlist.SuspendRefresh();
                ClearError(line);

                foreach (var item in items)
                {
                    tags.AddRange(CreateError(item));
                }

                return tags;
            }
            finally
            {
                _errorlist.ResumeRefresh();
                _errorlist.Refresh();
            }
        }

        private IEnumerable<TagSpan<ErrorTag>> CreateError(ParseItem item)
        {
            foreach (var error in item.Errors)
            {
                var span = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, item.Span);

                var task = CreateErrorTask(span, error);
                _errorlist.Tasks.Add(task);

                var errorType = GetErrorType(error.ErrorType);

                yield return new TagSpan<ErrorTag>(span, new ErrorTag(errorType, error.Name));
            }
        }

        public static string GetErrorType(ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.Error:
                    return PredefinedErrorTypeNames.SyntaxError;
                case ErrorType.Warning:
                    return PredefinedErrorTypeNames.Warning;
            }

            return PredefinedErrorTypeNames.OtherError;
        }

        private ErrorTask CreateErrorTask(SnapshotSpan span, Error error)
        {
            var line = span.Snapshot.GetLineFromPosition(span.Start);

            ErrorTask task = new ErrorTask
            {
                Text = error.Description,
                Line = line.LineNumber,
                Column = span.Start.Position - line.Start.Position,
                Category = TaskCategory.Misc,
                ErrorCategory = GetErrorCategory(error.ErrorType),
                Priority = TaskPriority.Low,
                Document = _file
            };

            task.Navigate += Navigate;

            return task;
        }

        private TaskErrorCategory GetErrorCategory(ErrorType errorType)
        {
            if (errorType == ErrorType.Message)
                return TaskErrorCategory.Message;

            if (errorType == ErrorType.Warning || EditorConfigPackage.ValidationOptions.ShowErrorsAsWarnings)
                return TaskErrorCategory.Warning;

            return TaskErrorCategory.Error;
        }

        private void Navigate(object sender, EventArgs e)
        {
            ErrorTask task = (ErrorTask)sender;
            _errorlist.Navigate(task, new Guid("{00000000-0000-0000-0000-000000000000}"));

            var line = _view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(task.Line);
            var point = new SnapshotPoint(line.Snapshot, line.Start.Position + task.Column);
            _view.Caret.MoveTo(point);
        }

        private void ClearError(ITextSnapshotLine line)
        {
            for (int i = _errorlist.Tasks.Count - 1; i >= 0; i--)
            {
                var task = _errorlist.Tasks[i];

                if (task.Line == line.LineNumber)
                    _errorlist.Tasks.RemoveAt(i);
            }
        }

        public void Dispose()
        {
            _validator.Validated -= DocumentValidated;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
