using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Linq;
using System.Text;

namespace EditorConfig
{
    internal sealed class EditorConfigFormatter : BaseCommand
    {
        private ITextBufferUndoManager _undoManager;
        private IWpfTextView _view;

        public EditorConfigFormatter(IWpfTextView textView, ITextBufferUndoManager undoManager)
        {
            _view = textView;
            _undoManager = undoManager;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                        FormatSpan(0, _view.TextBuffer.CurrentSnapshot.Length);
                        break;
                    case VSConstants.VSStd2KCmdID.FORMATSELECTION:
                        FormatSelection();
                        break;
                }
            }

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void FormatSelection()
        {
            int start = _view.Selection.Start.Position.Position;
            int length = _view.Selection.End.Position.Position - start;

            FormatSpan(start, length);
        }

        private void FormatSpan(int start, int length)
        {
            var sb = new StringBuilder();
            int emptyCount = 0;

            foreach (var line in _view.TextBuffer.CurrentSnapshot.Lines)
            {
                var text = line.GetText();
                var isEmpty = string.IsNullOrWhiteSpace(text);

                if (!isEmpty)
                {
                    var eq = text.IndexOf('=');
                    var clean = text.Trim();

                    if (eq > -1)
                        clean = string.Join(" = ", text.Split('=').Select(s => s.Trim()));

                    sb.AppendLine(clean);
                    emptyCount = 0;
                }
                else
                {
                    if (emptyCount < 1)
                    {
                        sb.AppendLine();
                    }

                    emptyCount++;
                }
            }

            using (var transaction = _undoManager.TextBufferUndoHistory.CreateTransaction($"Format"))
            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Replace(start, length, sb.ToString());
                edit.Apply();
                transaction.Complete();
            }
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.FORMATDOCUMENT:
                    case VSConstants.VSStd2KCmdID.FORMATSELECTION:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}