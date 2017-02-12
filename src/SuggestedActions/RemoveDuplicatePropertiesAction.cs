using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace EditorConfig
{
    class RemoveDuplicatePropertiesAction : BaseSuggestedAction
    {
        private Section _section;
        private ITextView _view;

        public RemoveDuplicatePropertiesAction(Section section, ITextView view)
        {
            _section = section;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Remove duplicates"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.CleanData; }
        }

        public override bool IsEnabled
        {
            get
            {
                return _section.Properties.Any(p => p.Keyword.Errors.Any(e => e.Name == ErrorCatalog.DuplicateProperty.Code || e.Name == ErrorCatalog.ParentDuplicateProperty.Code));
            }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = _view.Caret.Position.BufferPosition;
            IEnumerable<Property> duplicates = _section.Properties.Where(p => p.Keyword.Errors.Any(e => e.Name == ErrorCatalog.DuplicateProperty.Code || e.Name == ErrorCatalog.ParentDuplicateProperty.Code));

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                foreach (Property dupe in duplicates.Reverse())
                {
                    ITextSnapshotLine line = _view.TextBuffer.CurrentSnapshot.GetLineFromPosition(dupe.Span.Start);
                    edit.Delete(line.ExtentIncludingLineBreak);
                }

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPost));
        }
    }
}
