using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    class AddMissingRulesActionDotNet : BaseSuggestedAction
    {
        private readonly List<Keyword> _missingRules;
        private readonly EditorConfigDocument _document;
        private readonly ITextView _view;

        public AddMissingRulesActionDotNet(List<Keyword> missingRules, EditorConfigDocument document, ITextView view)
        {
            _missingRules = missingRules;
            _document = document;
            _view = view;
        }

        public override string DisplayText
        {
            get { return ".NET"; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = _view.Caret.Position.BufferPosition;

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                AddMissingRulesActionAll.AddMissingRules(_document, _missingRules, edit);

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPost));
        }
    }
}
