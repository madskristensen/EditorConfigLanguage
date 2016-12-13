using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using System.Linq;
using System.Text;
using System.Threading;

namespace EditorConfig
{
    class SortPropertiesAction : BaseSuggestedAction
    {
        private Section _section;
        private ITextBuffer _buffer;

        public SortPropertiesAction(Section section, ITextBuffer buffer)
        {
            _section = section;
            _buffer = buffer;
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
            using (var edit = _buffer.CreateEdit())
            {
                SortSection(_section, edit);

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }
        }

        public static void SortSection(Section section, ITextEdit edit)
        {
            var first = section.Properties.FirstOrDefault();
            var last = section.Properties.LastOrDefault();

            if (first == null)
                return;

            var bufferLines = edit.Snapshot.Lines.Where(l => l.Start >= first.Span.Start && l.End <= last.Span.End);
            var lines = bufferLines.Select(b => b.GetText());

            var properties = lines.OrderBy(l => l.IndexOf("csharp_") + l.IndexOf("dotnet_"))
                                  .ThenBy(p => p);

            var sb = new StringBuilder();
            sb.AppendLine(section.Item.Text);

            foreach (var property in properties)
            {
                sb.AppendLine(property);
            }

            edit.Replace(section.Span, sb.ToString().TrimEnd());
        }
    }
}
