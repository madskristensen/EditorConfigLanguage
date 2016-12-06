using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    class DeleteSectionAction : BaseSuggestedAction
    {
        private ITextBuffer _buffer;
        private Span _spanToDelete;

        public DeleteSectionAction(ITextBuffer buffer, Span spanToDelete)
        {
            _buffer = buffer;
            _spanToDelete = spanToDelete;
        }

        public override string DisplayText
        {
            get { return "Delete Section"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.Cancel; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            using (var edit = _buffer.CreateEdit())
            {
                edit.Delete(_spanToDelete);
                edit.Apply();
            }
        }
    }
}
