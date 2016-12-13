using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Threading;

namespace EditorConfig
{
    class SortAllPropertiesAction : BaseSuggestedAction
    {
        private EditorConfigDocument _document;

        public SortAllPropertiesAction(EditorConfigDocument document)
        {
            _document = document;
        }

        public override string DisplayText
        {
            get { return "Sort Properties in all sections"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.SortAscending; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            using (var edit = _document.TextBuffer.CreateEdit())
            {
                foreach (var section in _document.Sections)
                {
                    SortPropertiesAction.SortSection(section, edit);
                }

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }
        }
    }
}
