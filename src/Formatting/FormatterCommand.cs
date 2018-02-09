using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;

namespace EditorConfig
{
    internal sealed class FormatterCommand : BaseCommand
    {
        private ITextBufferUndoManager _undoManager;
        private IWpfTextView _view;

        public FormatterCommand(IWpfTextView textView, ITextBufferUndoManager undoManager)
        {
            _view = textView;
            _undoManager = undoManager;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT)
            {
                FormatDocument();
                return VSConstants.S_OK;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void FormatDocument()
        {
            using (ITextUndoTransaction transaction = _undoManager.TextBufferUndoHistory.CreateTransaction(Resources.Text.FormatDocument))
            {
                EditorConfigFormatter formatter = _view.Properties.GetOrCreateSingletonProperty(() => new EditorConfigFormatter(_view.TextBuffer));
                bool changed = formatter.Format();

                if (changed)
                    transaction.Complete();
                else
                    transaction.Cancel();
            }
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K && prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.FORMATDOCUMENT)
            {
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                return VSConstants.S_OK;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}