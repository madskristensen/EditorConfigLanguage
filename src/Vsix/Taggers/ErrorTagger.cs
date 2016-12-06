using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace EditorConfig
{
    class ErrorTagger : ITagger<IErrorTag>
    {
        private IClassifier _classifier;
        private ErrorListProvider _errorlist;
        private ITextDocument _document;
        private IWpfTextView _view;
        private bool _hasLoaded;

        public ErrorTagger(IWpfTextView view, IClassifierAggregatorService classifier, ErrorListProvider errorlist, ITextDocument document)
        {
            _view = view;
            _classifier = classifier.GetClassifier(view.TextBuffer);
            _errorlist = errorlist;
            _document = document;

            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                _hasLoaded = true;
                var span = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            });
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (!_hasLoaded || !spans.Any() || spans[0].IsEmpty)
                yield break;

            var span = spans[0];
            var line = span.Start.GetContainingLine();
            var classificationSpans = _classifier.GetClassificationSpans(line.Extent);
            string property = null;

            try
            {
                _errorlist.SuspendRefresh();
                ClearError(line);

                foreach (var cspan in classificationSpans)
                {
                    if (cspan.ClassificationType.IsOfType(EditorConfigClassificationTypes.Keyword))
                    {
                        property = cspan.Span.GetText();

                        var item = CompletionItem.GetCompletionItem(property);

                        if (item == null)
                            yield return CreateError(line, cspan, string.Format(Resources.Text.ValidateUnknownKeyword, property));
                    }
                    else if (cspan.ClassificationType.IsOfType(EditorConfigClassificationTypes.Value))
                    {
                        if (string.IsNullOrEmpty(property))
                            continue;

                        var item = CompletionItem.GetCompletionItem(property);

                        if (item == null)
                            continue;

                        string value = cspan.Span.GetText();

                        if (!item.Values.Contains(value, StringComparer.OrdinalIgnoreCase) && !(int.TryParse(value, out int intValue) && intValue > 0))
                            yield return CreateError(line, cspan, string.Format(Resources.Text.InvalidValue, value, property));

                        // C# style rules validation
                        if (!property.StartsWith("csharp_") && !property.StartsWith("dotnet_"))
                            continue;

                        var lineText = line.Extent.GetText().Trim();

                        if (lineText.EndsWith(":"))
                            yield return CreateError(line, cspan, string.Format(Resources.Text.ValidationInvalidEndChar, ":"));

                        if (lineText.EndsWith("true"))
                            yield return CreateError(line, cspan, Resources.Text.ValidationMissingSeverity);
                    }
                    else if (cspan.ClassificationType.IsOfType(EditorConfigClassificationTypes.Severity))
                    {
                        string severity = cspan.Span.GetText().Trim();

                        if (!Constants.Severity.Contains(severity))
                            yield return CreateError(line, cspan, string.Format(Resources.Text.ValidationInvalidSeverity, string.Join(", ", Constants.Severity)));
                    }
                }
            }
            finally
            {
                _errorlist.ResumeRefresh();
                _errorlist.Refresh();
            }
        }

        private TagSpan<ErrorTag> CreateError(ITextSnapshotLine line, ClassificationSpan cspan, string message)
        {
            ErrorTask task = CreateErrorTask(line, cspan, message);

            _errorlist.Tasks.Add(task);

            SnapshotSpan CheckTextSpan = new SnapshotSpan(cspan.Span.Snapshot, new Span(cspan.Span.Start, cspan.Span.Length));
            return new TagSpan<ErrorTag>(CheckTextSpan, new ErrorTag(message, message));
        }

        private ErrorTask CreateErrorTask(ITextSnapshotLine line, ClassificationSpan cspan, string text)
        {
            ErrorTask task = new ErrorTask
            {
                Text = text,
                Line = line.LineNumber,
                Column = cspan.Span.Start.Position - line.Start.Position,
                Category = TaskCategory.Misc,
                ErrorCategory = TaskErrorCategory.Warning,
                Priority = TaskPriority.Low,
                Document = _document.FilePath
            };

            task.Navigate += Navigate;

            return task;
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

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
