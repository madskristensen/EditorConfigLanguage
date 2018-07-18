using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    class AddMissingRulesActionAll : BaseSuggestedAction
    {
        private List<Keyword> _missingRules;
        private EditorConfigDocument _document;
        private ITextView _view;

        public AddMissingRulesActionAll(List<Keyword> missingRules, EditorConfigDocument document, ITextView view)
        {
            _missingRules = missingRules;
            _document = document;
            _view = view;
        }
        public override string DisplayText
        {
            get { return "All"; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = _view.Caret.Position.BufferPosition;

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                AddMissingRules(_document, _missingRules, edit);

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPost));
        }

        internal static void AddMissingRules(EditorConfigDocument document, List<Keyword> missingRules, ITextEdit edit)
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            bool firstRule = true;
            string curHeader = "";
            foreach (Keyword curRule in missingRules)
            {
                if (firstRule)
                {
                    curHeader = GetHeader(curRule);
                    sb.AppendLine(curHeader);
                    firstRule = false;
                }
                else if (!curHeader.Equals(GetHeader(curRule)))
                {
                    curHeader = GetHeader(curRule);
                    sb.AppendLine();
                    sb.AppendLine(curHeader);
                }

                sb.Append(curRule.Name + " = ");
                IEnumerator<Value> defaultValues = curRule.DefaultValue.GetEnumerator();
                bool firstValue = true;
                while (defaultValues.MoveNext())
                {
                    if (firstValue)
                    {
                        firstValue = false;
                    }
                    else
                    {
                        sb.Append(",");
                    }
                    sb.Append(defaultValues.Current.Name);
                }

                if (curRule.RequiresSeverity)
                {
                    sb.Append(":" + curRule.DefaultSeverity);
                }
                sb.AppendLine();
            };

            edit.Insert(document.TextBuffer.CurrentSnapshot.Length, sb.ToString());
        }

        private static string GetHeader(Keyword curRule)
        {
            switch (curRule.Category)
            {
                case Category.CSharp:
                    return "[*.cs]";
                case Category.DotNet:
                    return "[*.{cs,vb}]";
                case Category.VisualBasic:
                    return "[*.vb]";
                default:
                    return "[*]";
            }
        }
    }
}
