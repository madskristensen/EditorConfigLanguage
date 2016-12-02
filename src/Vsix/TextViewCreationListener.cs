using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Windows.Threading;

namespace EditorConfig
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory { get; set; }

        [Import]
        private ICompletionBroker CompletionBroker { get; set; }

        [Import]
        private SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        private ITextBufferUndoManagerProvider UndoProvider { get; set; }

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        private ErrorListProvider _errorList;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            view.TextBuffer.Properties.GetOrCreateSingletonProperty(() => view);
            _errorList = view.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new ErrorListProvider(ServiceProvider));

            if (_errorList == null)
                return;

            var undoManager = UndoProvider.GetTextBufferUndoManager(view.TextBuffer);

            AddCommandFilter(textViewAdapter, new EditorConfigFormatter(view, undoManager));
            AddCommandFilter(textViewAdapter, new CompletionController(view, CompletionBroker));
            AddCommandFilter(textViewAdapter, new F1Help());

            var viewEx = textViewAdapter as IVsTextViewEx;

            if (viewEx != null)
                ErrorHandler.ThrowOnFailure(viewEx.PersistOutliningState());

            if (DocumentService.TryGetTextDocument(view.TextBuffer, out var document))
            {
                document.FileActionOccurred += Document_FileActionOccurred;
            }

            view.Closed += OnViewClosed;
        }

        private void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
                {
                    var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                    statusBar.IsFrozen(out int frozen);

                    if (frozen == 0)
                    {
                        statusBar.SetText("Saved. Open documents must be reopened for .editorconfig changes to take effect");
                    }
                });
            }
        }

        private void AddCommandFilter(IVsTextView textViewAdapter, BaseCommand command)
        {
            textViewAdapter.AddCommandFilter(command, out var next);
            command.Next = next;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;
            view.Closed -= OnViewClosed;

            if (_errorList != null)
            {
                _errorList.Tasks.Clear();
                _errorList.Dispose();
                _errorList = null;
            }
        }
    }
}
