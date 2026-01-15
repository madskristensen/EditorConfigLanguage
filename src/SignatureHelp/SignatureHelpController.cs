using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;

namespace EditorConfig
{
    internal sealed class SignatureHelpCommand : BaseCommand
    {
        private readonly Guid _commandGroup = typeof(VSConstants.VSStd2KCmdID).GUID;
        private const uint _commandId = (uint)VSConstants.VSStd2KCmdID.TYPECHAR;
        private readonly IWpfTextView _view;
        private readonly ISignatureHelpBroker _signaturehelpBroker;
        private readonly IAsyncQuickInfoBroker _quickInfoBroker;
        private ISignatureHelpSession _session;

        public SignatureHelpCommand(IWpfTextView view, ISignatureHelpBroker signaturehelpBroker, IAsyncQuickInfoBroker quickInfoBroker)
        {
            _view = view;
            _signaturehelpBroker = signaturehelpBroker;
            _quickInfoBroker = quickInfoBroker;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == _commandGroup && nCmdID == _commandId && EditorConfigPackage.Language.Preferences.ParameterInformation)
            {
                char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                if (typedChar == 27 && _session != null)
                {
                    DismissSession();
                }
                else if (typedChar == '[')
                {
                    TriggerSession();
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
            {
                DismissSession();
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.PARAMINFO)
            {
                TriggerSession();
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void TriggerSession()
        {
            if (_session == null || _session.IsDismissed)
            {
                IAsyncQuickInfoSession quickInfoSession = _quickInfoBroker.GetSession(_view);
                if (quickInfoSession != null)
                    ThreadHelper.JoinableTaskFactory.Run(async () => await quickInfoSession.DismissAsync());

                _ = ThreadHelper.JoinableTaskFactory.StartOnIdle(
                    () =>
                    {
                        _session = _signaturehelpBroker.TriggerSignatureHelp(_view);
                        if (_session != null)
                            _session.Match();

                        return Task.CompletedTask;
                    },
                    VsTaskRunContext.UIThreadNormalPriority);
            }
        }

        private void DismissSession()
        {
            if (_session != null)
            {
                _session.Dismiss();
                _session = null;
            }
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