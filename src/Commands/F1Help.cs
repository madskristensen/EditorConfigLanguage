using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Text.RegularExpressions;

namespace EditorConfig
{
    internal sealed class F1Help : BaseCommand
    {
        private Guid _commandGroup = typeof(VSConstants.VSStd97CmdID).GUID;
        private const uint _commandId = (uint)VSConstants.VSStd97CmdID.F1Help;
        private IVsTextView _vsTextView;
        private IWpfTextView _view;

        public F1Help(IVsTextView textViewAdapter, IWpfTextView view)
        {
            _vsTextView = textViewAdapter;
            _view = view;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == _commandGroup && nCmdID == _commandId)
            {
                _vsTextView.GetCaretPos(out int line, out int column);
                string curLine = _view.TextSnapshot.GetLineFromLineNumber(line).GetText();

                var pattern = new Regex(@"([\w .]+)=");
                if (pattern.IsMatch(curLine))
                {
                    GroupCollection groups = pattern.Match(curLine).Groups;
                    string ruleName = groups[1].Value.ToString().Trim();

                    SchemaCatalog.TryGetKeyword(ruleName, out Keyword keyword);
                    if (keyword != null && keyword.DocumentationLink != null)
                    {
                        VsShellUtilities.OpenSystemBrowser(keyword.DocumentationLink);
                    }
                    else
                    {
                        VsShellUtilities.OpenSystemBrowser(Constants.Homepage);
                    }
                }
                else
                {
                    VsShellUtilities.OpenSystemBrowser(Constants.Homepage);
                }
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