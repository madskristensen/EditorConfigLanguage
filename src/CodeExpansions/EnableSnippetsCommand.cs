using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace EditorConfig
{
    internal class EnableSnippetsCommand : BaseCommand, IVsExpansionClient
    {
        private const string _guid = EditorConfigLanguage.LanguageGuid;
        private IVsTextView _vsTextView;
        private ITextView _view;
        private IVsExpansionManager _manager;
        private IVsExpansionSession _session;
        private ITextStructureNavigatorSelectorService _navigator;

        internal EnableSnippetsCommand(IVsTextView vsTextView, ITextView textView, ITextStructureNavigatorSelectorService navigator)
        {
            _vsTextView = vsTextView;
            _view = textView;
            _navigator = navigator;

            var textManager = (IVsTextManager2)Package.GetGlobalService(typeof(SVsTextManager));
            ErrorHandler.ThrowOnFailure(textManager.GetExpansionManager(out _manager));

            _session = null;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            //the snippet picker code starts here
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
            {
                _manager.InvokeInsertionUI(
                    _vsTextView,
                    this,      //the expansion client
                    new Guid(_guid),
                    null,       //use all snippet types
                    0,          //number of types (0 for all)
                    0,          //ignored if iCountTypes == 0
                    null,       //use all snippet kinds
                    0,          //use all snippet kinds
                    0,          //ignored if iCountTypes == 0
                    Constants.LanguageName, //the text to show in the prompt
                    string.Empty);  //only the ENTER key causes insert

                return VSConstants.S_OK;
            }

            if (_session != null)
            {
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKTAB)
                {
                    _session.GoToPreviousExpansionField();
                    return VSConstants.S_OK;
                }
                else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                {
                    _session.GoToNextExpansionField(0); //false to support cycling through all the fields
                    return VSConstants.S_OK;
                }
                else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
                {
                    if (_session.EndCurrentExpansion(0) == VSConstants.S_OK)
                    {
                        _session = null;
                        return VSConstants.S_OK;
                    }
                }
            }

            if (_session == null && nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
            {
                CaretPosition pos = _view.Caret.Position;
                TextExtent word = _navigator.GetTextStructureNavigator(_view.TextBuffer).GetExtentOfWord(pos.BufferPosition - 1);
                string textString = word.Span.GetText();

                if (InsertAnyExpansion(textString, null, null))
                {
                    EndSession();
                    return VSConstants.S_OK;
                }
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K && cCmds > 0)
            {
                // make the Insert Snippet command appear on the context menu
                if (prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
                {
                    prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                    return VSConstants.S_OK;
                }
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int GetExpansionFunction(global::MSXML.IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            pFunc = null;
            return VSConstants.S_OK;
        }

        public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
        {
            return VSConstants.S_OK;
        }

        public int EndExpansion()
        {
            return VSConstants.S_OK;
        }

        public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
        {
            pfIsValidType = 1;
            return VSConstants.S_OK;
        }

        public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
        {
            pfIsValidKind = 1;
            return VSConstants.S_OK;
        }

        public int OnBeforeInsertion(IVsExpansionSession pSession)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterInsertion(IVsExpansionSession pSession)
        {
            return VSConstants.S_OK;
        }

        public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts)
        {
            return VSConstants.S_OK;
        }

        public int OnItemChosen(string pszTitle, string pszPath)
        {
            if (InsertAnyExpansion(null, pszTitle, pszPath))
            {
                EndSession();
            }

            return VSConstants.S_OK;
        }

        private void EndSession()
        {
            if (_session != null)
            {
                _session.EndCurrentExpansion(0);
                _session = null;
            }
        }

        private bool InsertAnyExpansion(string shortcut, string title, string path)
        {
            _vsTextView.GetCaretPos(out int startLine, out int endColumn);

            var addSpan = new TextSpan()
            {
                iStartIndex = endColumn,
                iEndIndex = endColumn,
                iStartLine = startLine,
                iEndLine = startLine
            };

            if (shortcut != null)
            {
                addSpan.iStartIndex = addSpan.iEndIndex - shortcut.Length;

                _manager.GetExpansionByShortcut(
                    this,
                    new Guid(_guid),
                    shortcut,
                    _vsTextView,
                    new TextSpan[] { addSpan },
                    0,
                    out path,
                    out title);

            }
            if (title != null && path != null)
            {
                _vsTextView.GetBuffer(out IVsTextLines textLines);
                var bufferExpansion = (IVsExpansion)textLines;

                if (bufferExpansion != null)
                {
                    int hr = bufferExpansion.InsertNamedExpansion(
                        title,
                        path,
                        addSpan,
                        this,
                        new Guid(_guid),
                        0,
                       out _session);

                    if (VSConstants.S_OK == hr)
                    {
                        Telemetry.TrackUserTask("SnippetInserted");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
