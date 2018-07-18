using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    class AddMissingRulesActionVB : BaseSuggestedAction
    {
        private List<Keyword> _missingRules;
        private EditorConfigDocument _document;
        private ITextView _view;

        public AddMissingRulesActionVB(List<Keyword> missingRules, EditorConfigDocument document, ITextView view)
        {
            _missingRules = missingRules;
            _document = document;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Basic"; }
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
