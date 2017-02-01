using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace EditorConfig
{
    class SortPropertiesAction : BaseSuggestedAction
    {
        private Section _section;
        private ITextView _view;

        public SortPropertiesAction(Section section, ITextView view)
        {
            _section = section;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Sort Properties in Section"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.SortAscending; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = _view.Caret.Position.BufferPosition;

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                SortSection(_section, edit);

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPost));
        }

        public static void SortSection(Section section, ITextEdit edit)
        {
            Property first = section.Properties.FirstOrDefault();
            Property last = section.Properties.LastOrDefault();

            if (first == null)
                return;

            IEnumerable<ITextSnapshotLine> bufferLines = edit.Snapshot.Lines.Where(l => l.Start >= first.Span.Start && l.End <= last.Span.End);
            IEnumerable<string> lines = bufferLines.Select(b => b.GetText());

            IOrderedEnumerable<string> properties = lines.OrderBy(l => l.IndexOf("csharp_") + l.IndexOf("dotnet_"))
                                  .ThenBy(p => p);

            var sb = new StringBuilder();
            sb.AppendLine(section.Item.Text);

            foreach (string property in properties.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                sb.AppendLine(property);
            }

            edit.Replace(section.Span, sb.ToString().TrimEnd());
        }
    }
}
