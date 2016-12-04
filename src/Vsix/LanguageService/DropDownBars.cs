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
using System.Linq;

namespace EditorConfig
{
    public class DropDownBars : TypeAndMemberDropdownBars
    {
        private readonly LanguageService _languageService;
        private readonly ITextView _textView;
        private List<DropDownItem> _members;

        public DropDownBars(LanguageService languageService, IVsTextView view)
        : base(languageService)
        {
            _languageService = languageService;

            var componentModel = (IComponentModel)languageService.GetService(typeof(SComponentModel));
            var editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _textView = editorAdapterFactoryService.GetWpfTextView(view);

            _textView.Caret.PositionChanged += CaretPositionChanged;
            _textView.TextBuffer.PostChanged += TextBufferChanged;

            UpdateElements(_textView.TextSnapshot, true);
        }

        private void TextBufferChanged(object sender, EventArgs e)
        {
            UpdateElements(_textView.TextSnapshot, true);
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

            var localMembers = _members;

            foreach (var item in localMembers)
            {
                dropDownMembers.Add(new DropDownMember(item.Name, item.Span, 0, DROPDOWNFONTATTR.FONTATTR_PLAIN));
            }

            var currentMember = localMembers.LastOrDefault(m => m.Span.iStartLine <= line);

            if (currentMember != null)
            {
                selectedMember = localMembers.IndexOf(currentMember);
            }

            if (dropDownTypes.Count == 0)
            {
                dropDownTypes.Add(new DropDownMember(Constants.FileName, new TextSpan(), 0, DROPDOWNFONTATTR.FONTATTR_BOLD));
            }

            return true;
        }

        private void UpdateElements(ITextSnapshot snapshot, bool synchronize)
        {
            var list = new List<DropDownItem>();

            foreach (var line in snapshot.Lines)
            {
                string text = line.GetText();

                if (text.StartsWith("[", StringComparison.Ordinal))
                {
                    if (line.LineNumber > 0 && !list.Any())
                        list.Add(new DropDownItem("<Root>", 1));

                    list.Add(new DropDownItem("   " + text.Trim(), line.LineNumber));
                }
            }

            _members = list;

            if (synchronize)
            {
                SyncDropDowns();
            }
        }

        private void SyncDropDowns()
        {
            ThreadHelper.Generic.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () =>
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
