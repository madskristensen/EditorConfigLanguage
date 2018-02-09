using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Runtime.InteropServices;

namespace EditorConfig
{
    internal sealed class CompletionController : BaseCommand
    {
        private ICompletionSession _currentSession;
        private IQuickInfoBroker _quickInfoBroker;

        public CompletionController(IWpfTextView textView, ICompletionBroker broker, IQuickInfoBroker quickInfoBroker)
        {
            _currentSession = null;
            _quickInfoBroker = quickInfoBroker;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; }
        public ICompletionBroker Broker { get; }
        public static bool ShowAllMembers { get; private set; }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool handled = false;
            int hresult = VSConstants.S_OK;

            // 1. Pre-process
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        ShowAllMembers = false;
                        handled = CompleteWord();
                        break;
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                        ShowAllMembers = true;
                        handled = StartSession();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = Commit(false);
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Commit(true);
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Dismiss();
                        break;
                }
            }

            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (ErrorHandler.Succeeded(hresult))
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            ShowAllMembers = false;
                            HandleTypeChar(pvaIn);
                            break;
                        case VSConstants.VSStd2KCmdID.BACKSPACE:
                        case VSConstants.VSStd2KCmdID.DELETE:
                            Filter();
                            break;
                    }
                }
            }

            return hresult;
        }

        private void HandleTypeChar(IntPtr pvaIn)
        {
            bool handled = false;

            if (EditorConfigPackage.Language.Preferences.AutoListMembers)
            {
                char ch = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                if (char.IsLetterOrDigit(ch) && EditorConfigPackage.Language.Preferences.AutoListMembers)
                {
                    StartSession();
                    handled = true;
                }
                else if ((ch == ':' || ch == '=' || ch == ' ' || ch == ',') && EditorConfigPackage.Language.Preferences.AutoListMembers)
                {
                    Dismiss();
                    StartSession();
                    handled = true;
                }
            }

            if (!handled && _currentSession != null)
            {
                Filter();
            }
        }

        private void Filter()
        {
            if (_currentSession != null)
            {
                _currentSession.SelectedCompletionSet.SelectBestMatch();
            }
        }

        bool Dismiss()
        {
            if (_currentSession == null)
                return false;

            _currentSession.Dismiss();

            return true;
        }

        bool Commit(bool force)
        {
            if (_currentSession == null)
                return false;

            if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                _currentSession.Dismiss();
                return false;
            }
            else
            {
                string moniker = _currentSession.SelectedCompletionSet.Moniker;
                _currentSession.Commit();

                if (!EditorConfigPackage.CompletionOptions.AutoInsertDelimiters)
                    return true;

                SnapshotPoint position = TextView.Caret.Position.BufferPosition;
                ITextSnapshotLine line = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
                string lineText = line.GetText();

                if (moniker == "keyword" && !lineText.Contains("="))
                {
                    TextView.TextBuffer.Insert(position, " = ");

                    // Contains placeholders
                    int start = lineText.IndexOf('<');
                    int end = lineText.IndexOf('>');

                    if (start > -1 && start < end)
                    {
                        var span = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, Span.FromBounds(line.Start + start, line.Start + end + 1));
                        TextView.Selection.Select(span, false);
                        TextView.Caret.MoveTo(span.Start);
                        return true;
                    }

                    if (EditorConfigPackage.Language.Preferences.AutoListMembers)
                        StartSession();
                }
                else if (moniker == "value" && !lineText.Contains(":"))
                {
                    var document = EditorConfigDocument.FromTextBuffer(TextView.TextBuffer);
                    Property prop = document.PropertyAtPosition(position - 1);

                    if (SchemaCatalog.TryGetKeyword(prop.Keyword.Text, out Keyword keyword) && prop.Value != null)
                    {
                        if (keyword.RequiresSeverity)
                        {
                            TextView.TextBuffer.Insert(position, ":");

                            if (EditorConfigPackage.Language.Preferences.AutoListMembers)
                                StartSession();
                        }
                    }
                }

                return true;
            }
        }

        bool StartSession()
        {
            if (_currentSession != null)
                return false;

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
            ITextSnapshot snapshot = caret.Snapshot;

            if (!Broker.IsCompletionActive(TextView))
            {
                _currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
            }
            else
            {
                _currentSession = Broker.GetSessions(TextView)[0];
            }

            _currentSession.Dismissed += (sender, args) => _currentSession = null;
            _currentSession.Start();

            if (_quickInfoBroker.IsQuickInfoActive(TextView))
            {
                foreach (IQuickInfoSession session in _quickInfoBroker.GetSessions(TextView))
                {
                    session.Dismiss();
                }
            }

            return true;
        }

        private bool CompleteWord()
        {
            StartSession();

            if (_currentSession == null || _currentSession.CompletionSets.Count == 0)
                return false;

            if (_currentSession.CompletionSets[0].Completions.Count == 1)
            {
                string text = _currentSession.CompletionSets[0].ApplicableTo.GetText(TextView.TextSnapshot);

                if (!text.Equals(_currentSession.CompletionSets[0].Completions[0].DisplayText, StringComparison.OrdinalIgnoreCase))
                    return Commit(true);

                ShowAllMembers = true;
                Dismiss();
                StartSession();
            }
            else
            {
                Filter();
            }

            return false;
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                    case VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}