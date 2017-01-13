using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
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
        private ISignatureHelpBroker _broker;
        private ISignatureHelpSession _session;

        public SignatureHelpCommand(IWpfTextView view, ISignatureHelpBroker broker)
        {
            _view = view;
            _broker = broker;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == _commandGroup && nCmdID == _commandId && EditorConfigPackage.Language.Preferences.ParameterInformation)
            {
                var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                if (typedChar == 27 && _session != null)
                {
                    DismissSession();
                }
                else if (typedChar == '[' && (_session == null || _session.IsDismissed))
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        _session = _broker.TriggerSignatureHelp(_view);
                    }), DispatcherPriority.ApplicationIdle, null);
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
            {
                DismissSession();
            }

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
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

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}