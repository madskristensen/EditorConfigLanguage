using System;
using System.ComponentModel.Composition;
using System.Linq;

using EnvDTE;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class ErrorList : IWpfTextViewCreationListener
    {
        [Import]
        private readonly ITextDocumentFactoryService _documentService = null;

        public void TextViewCreated(IWpfTextView view)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_documentService.TryGetTextDocument(view.TextBuffer, out ITextDocument doc))
                return;

            DocumentController controller = view.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new DocumentController(view.TextBuffer, doc.FilePath));
            controller.Attach(view);
        }

        private sealed class DocumentController : IDisposable
        {
            private readonly EditorConfigValidator _validator;
            private readonly EditorConfigDocument _document;
            private readonly TableDataSource _table;
            private readonly string _file;
            private readonly string _projectName;
            private int _views;
            private volatile bool _disposed;

            internal DocumentController(ITextBuffer buffer, string file)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _file = file;
                _document = EditorConfigDocument.FromTextBuffer(buffer);
                _validator = EditorConfigValidator.FromDocument(_document);
                _projectName = VsHelpers.DTE.Solution?.FindProjectItem(file)?.ContainingProject?.Name;
                _table = TableDataSource.Instance;
                _validator.Validated += Validated;
            }

            internal void Attach(IWpfTextView view)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _views++;
                view.Closed += ViewClosed;
                UpdateErrorList();
            }

            private void Validated(object sender, EventArgs e)
            {
                ThreadHelper.JoinableTaskFactory.StartOnIdle(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    UpdateErrorList();
                }, VsTaskRunContext.UIThreadIdlePriority).FileAndForget("EditorConfig/UpdateErrorList");
            }

            private void UpdateErrorList()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                ITextSnapshot snapshot = _document.TextBuffer.CurrentSnapshot;
                if (_disposed || _validator.IsValidating || !_validator.HasValidatedSnapshot(snapshot) ||
                    !_document.TryGetParseResult(snapshot, out EditorConfigDocument.ParseResult result))
                    return;

                _table.AddErrors(result.Items.Where(item => item.HasErrors), _projectName, _file);
            }

            private void ViewClosed(object sender, EventArgs e)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var view = (IWpfTextView)sender;
                view.Closed -= ViewClosed;
                if (--_views == 0)
                {
                    Dispose();
                    view.TextBuffer.Properties.RemoveProperty(typeof(DocumentController));
                }
            }

            public void Dispose()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _disposed = true;
                _validator.Validated -= Validated;
                _table.CleanErrors(_file);
            }
        }
    }
}
