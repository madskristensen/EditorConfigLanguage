using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Threading;

namespace EditorConfig
{
    class SortAllPropertiesAction : BaseSuggestedAction
    {
        private EditorConfigDocument _document;
        private ITextView _view;

        public SortAllPropertiesAction(EditorConfigDocument document, ITextView view)
        {
            _document = document;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Sort Properties in All Sections"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.SortAscending; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = _view.Caret.Position.BufferPosition;

            using (ITextEdit edit = _document.TextBuffer.CreateEdit())
            {
                foreach (Section section in _document.Sections)
                {
                    SortPropertiesAction.SortSection(section, edit);
                }

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPost));
        }
    }
}
