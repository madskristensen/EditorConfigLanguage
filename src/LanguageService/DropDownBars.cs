using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace EditorConfig
{
    public class DropDownBars : TypeAndMemberDropdownBars
    {
        private readonly LanguageService _languageService;
        private readonly ITextBuffer _buffer;
        private List<DropDownItem> _members;
        private EditorConfigDocument _document;

        public DropDownBars(LanguageService languageService, IVsTextView view)
        : base(languageService)
        {
            _languageService = languageService;

            var componentModel = (IComponentModel)languageService.GetService(typeof(SComponentModel));
            IVsEditorAdaptersFactoryService editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            IWpfTextView textView = editorAdapterFactoryService.GetWpfTextView(view);
            textView.Caret.PositionChanged += CaretPositionChanged;

            _buffer = textView.TextBuffer;

            _document = EditorConfigDocument.FromTextBuffer(_buffer);
            _document.Parsed += DocumentParsed;

            UpdateElements();
        }

        private void DocumentParsed(object sender, EventArgs e)
        {
            UpdateElements();
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            SyncDropDowns();
        }

        public override int GetComboAttributes(int combo, out uint entries, out uint entryType, out IntPtr imageList)
        {
            entries = 0;
            imageList = IntPtr.Zero;
            entryType = (uint)DROPDOWNENTRYTYPE.ENTRY_ATTR;

            switch (combo)
            {
                case 0:
                    entries = 1;
                    break;

                case 1:
                    entries = (uint)_members.Count;
                    break;
            }

            return VSConstants.S_OK;
        }

        public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView textView, int line, int col, ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
        {
            selectedType = 0;
            selectedMember = 0;
            dropDownMembers.Clear();

            List<DropDownItem> localMembers = _members;

            foreach (DropDownItem item in localMembers)
            {
                dropDownMembers.Add(new DropDownMember(item.Name, item.Span, 0, DROPDOWNFONTATTR.FONTATTR_PLAIN));
            }

            DropDownItem currentMember = localMembers.LastOrDefault(m => m.Span.iStartLine <= line);

            if (currentMember != null)
            {
                selectedMember = localMembers.IndexOf(currentMember);
            }

            if (dropDownTypes.Count == 0)
            {
                string type = $"{Constants.FileName} ({new FileInfo(_document.FileName).Directory.Name})";
                dropDownTypes.Add(new DropDownMember(type, new TextSpan(), 0, DROPDOWNFONTATTR.FONTATTR_BOLD));
            }

            return true;
        }

        private void UpdateElements()
        {
            var list = new List<DropDownItem>();

            foreach (Section section in _document.Sections)
            {
                int lineNumber = _buffer.CurrentSnapshot.GetLineNumberFromPosition(section.Span.Start);

                if (lineNumber > 0 && !list.Any())
                    list.Add(new DropDownItem("<Root>", 1));


                list.Add(new DropDownItem("   " + section.Item.Text, lineNumber));
            }

            _members = list;

            SyncDropDowns();
        }

        private void SyncDropDowns()
        {
            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                _languageService.SynchronizeDropdowns();
            });
        }
    }

    public class DropDownItem
    {
        public DropDownItem(string name, int lineNumber)
        {
            Name = name;
            Span = new TextSpan
            {
                iStartIndex = 0,
                iEndIndex = 0,
                iStartLine = lineNumber,
                iEndLine = lineNumber
            };
        }

        public string Name { get; set; }
        public TextSpan Span { get; set; }
    }
}
