using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(ContentTypes.EditorConfig)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        [Import(typeof(ITextBufferUndoManagerProvider))]
        private ITextBufferUndoManagerProvider UndoProvider { get; set; }

        private ErrorListProvider _errorList;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            view.TextBuffer.Properties.GetOrCreateSingletonProperty(() => view);
            _errorList = view.TextBuffer.Properties.GetOrCreateSingletonProperty(() => new ErrorListProvider(ServiceProvider));

            if (_errorList == null)
                return;

            var filter = new CompletionController(view, CompletionBroker);
            textViewAdapter.AddCommandFilter(filter, out var completionNext);
            filter.Next = completionNext;

            var undoManager = UndoProvider.GetTextBufferUndoManager(view.TextBuffer);

            var formatter = new EditorConfigFormatter(view, undoManager);
            textViewAdapter.AddCommandFilter(formatter, out var formatterNext);
            formatter.Next = formatterNext;

            view.Closed += OnViewClosed;
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;
            view.Closed -= OnViewClosed;

            if (_errorList != null)
            {
                _errorList.Tasks.Clear();
                _errorList.Dispose();
            }
        }
    }
}
