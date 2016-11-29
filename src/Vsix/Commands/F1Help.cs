using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using System;

namespace EditorConfig
{
    internal sealed class F1Help : BaseCommand
    {
        private Guid _commandGroup = typeof(VSConstants.VSStd97CmdID).GUID;

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == _commandGroup)
            {
                if  ((VSConstants.VSStd97CmdID)nCmdID == VSConstants.VSStd97CmdID.F1Help)
                {
                    System.Diagnostics.Process.Start(Constants.Homepage);
                    return VSConstants.S_OK;
                }
            }

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }


        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == _commandGroup)
            {
                switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd97CmdID.F1Help:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}