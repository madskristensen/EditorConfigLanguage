using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace EditorConfig
{
    internal sealed class SignatureHelpCommand : BaseCommand
    {
        private Guid _commandGroup = typeof(VSConstants.VSStd2KCmdID).GUID;
        private const uint _commandId = (uint)VSConstants.VSStd2KCmdID.TYPECHAR;
        private IWpfTextView _view;
        private ISignatureHelpBroker _signaturehelpBroker;
        private IQuickInfoBroker _quickInfoBroker;
        private ISignatureHelpSession _session;

        public SignatureHelpCommand(IWpfTextView view, ISignatureHelpBroker signaturehelpBroker, IQuickInfoBroker quickInfoBroker)
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
                if (_quickInfoBroker.IsQuickInfoActive(_view))
                    _quickInfoBroker.GetSessions(_view)[0].Dismiss();

                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    _session = _signaturehelpBroker.TriggerSignatureHelp(_view);
                    if (_session != null)
                        _session.Match();
                }), DispatcherPriority.Normal, null);
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