using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using Microsoft.VisualStudio.Shell;

namespace EditorConfig
{
    internal sealed class F1Help : BaseCommand
    {
        private Guid _commandGroup = typeof(VSConstants.VSStd97CmdID).GUID;
        private const uint _commandId = (uint)VSConstants.VSStd97CmdID.F1Help;

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == _commandGroup && nCmdID == _commandId)
            {
                VsShellUtilities.OpenSystemBrowser(Constants.Homepage);
                return VSConstants.S_OK;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == _commandGroup && prgCmds[0].cmdID == _commandId)
            {
                prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                return VSConstants.S_OK;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}