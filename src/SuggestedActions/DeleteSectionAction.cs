using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using System.Linq;

namespace EditorConfig
{
    class DeleteSectionAction : BaseSuggestedAction
    {
        private ITextBuffer _buffer;
        private Section _section;

        public DeleteSectionAction(ITextBuffer buffer, Section section)
        {
            _buffer = buffer;
            _section = section;
        }

        public override string DisplayText
        {
            get { return "Remove Section"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.Cancel; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            ITextSnapshotLine first = _buffer.CurrentSnapshot.GetLineFromPosition(_section.Span.Start);
            ITextSnapshotLine last = _buffer.CurrentSnapshot.GetLineFromPosition(_section.Span.End);

            if (_buffer.CurrentSnapshot.LineCount > last.LineNumber + 1)
            {
                ITextSnapshotLine nextLine = _buffer.CurrentSnapshot.GetLineFromLineNumber(last.LineNumber + 1);
                if (nextLine.Extent.IsEmpty)
                    last = nextLine;
            }

            using (ITextEdit edit = _buffer.CreateEdit())
            {
                edit.Delete(Span.FromBounds(first.Start, last.EndIncludingLineBreak));
                edit.Apply();
            }
        }
    }
}
