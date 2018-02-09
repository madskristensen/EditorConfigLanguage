using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace EditorConfig
{
    internal sealed class HideDefaultCommands : BaseCommand
    {
        private static Guid _commandGuid = typeof(VSConstants.VSStd97CmdID).GUID;
        private static HashSet<uint> _commands = new HashSet<uint>
        {
            (uint)VSConstants.VSStd97CmdID.GotoDefn,
            (uint)VSConstants.VSStd97CmdID.GotoDecl,
            (uint)VSConstants.VSStd97CmdID.GotoRef,
            (uint)VSConstants.VSStd97CmdID.FindReferences,
            (uint)VSConstants.VSStd97CmdID.RunToCursor,
        };

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == _commandGuid && _commands.Contains(prgCmds[0].cmdID))
            {
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_INVISIBLE;
                return VSConstants.S_OK;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}