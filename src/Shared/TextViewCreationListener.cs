using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
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

        [Import]
        private IQuickInfoBroker QuickInfoBroker { get; set; }

        [Import]
        ISignatureHelpBroker SignatureHelpBroker { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        private ITextBuffer _buffer;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            Telemetry.TrackOperation("FileOpened");
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            _buffer = view.TextBuffer;

            _buffer.Properties.GetOrCreateSingletonProperty(() => view);

            ITextBufferUndoManager undoManager = UndoProvider.GetTextBufferUndoManager(view.TextBuffer);

            AddCommandFilter(textViewAdapter, new FormatterCommand(view, undoManager));
            AddCommandFilter(textViewAdapter, new CompletionController(view, CompletionBroker, QuickInfoBroker));
            AddCommandFilter(textViewAdapter, new F1Help());
            AddCommandFilter(textViewAdapter, new NavigateToParent(_buffer));
            AddCommandFilter(textViewAdapter, new SignatureHelpCommand(view, SignatureHelpBroker, QuickInfoBroker));
            AddCommandFilter(textViewAdapter, new HideDefaultCommands());
            AddCommandFilter(textViewAdapter, new EnableSnippetsCommand(textViewAdapter, view, NavigatorService));

            if (textViewAdapter is IVsTextViewEx viewEx)
                ErrorHandler.ThrowOnFailure(viewEx.PersistOutliningState());

            if (DocumentService.TryGetTextDocument(_buffer, out ITextDocument document))
            {
                document.FileActionOccurred += DocumentSavedAsync;
            }

            view.Closed += OnViewClosed;
        }

        private async void DocumentSavedAsync(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            if (_buffer != null && _buffer.Properties.TryGetProperty(typeof(EditorConfigValidator), out EditorConfigValidator val))
            {
                await val.RequestValidationAsync(true);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusBar.IsFrozen(out int frozen);

            if (frozen == 0)
            {
                statusBar.SetText("Saved. Open documents must be reopened for .editorconfig changes to take effect");
            }
        }

        private void AddCommandFilter(IVsTextView textViewAdapter, BaseCommand command)
        {
            textViewAdapter.AddCommandFilter(command, out IOleCommandTarget next);
            command.Next = next;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;
            view.Closed -= OnViewClosed;

            if (view.TextBuffer.Properties.TryGetProperty(typeof(EditorConfigDocument), out EditorConfigDocument doc))
            {
                doc.Dispose();
            }

            if (view.TextBuffer.Properties.TryGetProperty(typeof(EditorConfigValidator), out EditorConfigValidator val))
            {
                val.Dispose();
            }
        }
    }
}
