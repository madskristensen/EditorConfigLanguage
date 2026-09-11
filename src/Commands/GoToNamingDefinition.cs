using System;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    internal sealed class GoToNamingDefinition(IWpfTextView view) : BaseCommand
    {
        private static readonly Guid _commandGroup = typeof(VSConstants.VSStd97CmdID).GUID;
        private const uint _commandId = (uint)VSConstants.VSStd97CmdID.GotoDefn;

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup != _commandGroup || nCmdID != _commandId ||
                !TryGetDefinition(out NamingSymbol definition))
            {
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, definition.Span.Start));
            view.Caret.EnsureVisible();
            return VSConstants.S_OK;
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == _commandGroup && prgCmds[0].cmdID == _commandId && TryGetDefinition(out _))
            {
                prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                return VSConstants.S_OK;
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private bool TryGetDefinition(out NamingSymbol definition)
        {
            definition = null;
            EditorConfigDocument document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            int position = view.Caret.Position.BufferPosition.Position;

            return NamingSymbolService.TryGetSymbolAtPosition(document, position, out NamingSymbol symbol) &&
                   NamingSymbolService.TryGetDefinition(document, symbol, out definition);
        }
    }
}
