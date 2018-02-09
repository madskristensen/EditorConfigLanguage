using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using System;

namespace EditorConfig
{
    internal sealed class NavigateToParent : BaseCommand
    {
        private Guid _commandGroup = PackageGuids.guidEditorConfigPackageCmdSet;
        private const uint _commandId = PackageIds.NavigateToParentId;
        private ITextBuffer _buffer;

        public NavigateToParent(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == _commandGroup && nCmdID == _commandId)
            {
                var document = EditorConfigDocument.FromTextBuffer(_buffer);
                EditorConfigDocument parent = document?.Parent;

                if (parent != null)
                {
                    VsHelpers.PreviewDocument(parent.FileName);
                }
                else
                {
                    var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                    statusBar.IsFrozen(out int frozen);

                    if (frozen == 0)
                    {
                        statusBar.SetText("This is a root document with no inheritance");
                    }
                }

                return VSConstants.S_OK;
            }

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }


        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == _commandGroup && prgCmds[0].cmdID == _commandId)
            {
                var document = EditorConfigDocument.FromTextBuffer(_buffer);
                EditorConfigDocument parent = document?.Parent;

                if (parent != null)
                {
                    prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                    return VSConstants.S_OK;
                }

                return VSConstants.S_OK;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}