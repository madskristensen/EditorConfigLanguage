using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    public class DisplayError : ITooltip
    {
        private Error _error;
        private ParseItem _item;

        public DisplayError(Error error, ParseItem item)
        {
            _error = error;
            _item = item;
            Description = _error.Description;
        }

        /// <summary>The error code is displayed in the Error List.</summary>
        public string Name => _error.Code;

        /// <summary>A short description of the error.</summary>
        public string Description { get; private set; }

        /// <summary>The error category determines how to display the error in the Error List.</summary>
        public ErrorCategory Category => _error.Category;

        /// <summary>A URL pointing to documentation about the error.</summary>
        public string HelpLink => string.Format(Constants.HelpLink, Name.ToLowerInvariant());

        /// <summary>The line number containing the error.</summary>
        public int Line { get; private set; } = 0;

        /// <summary>The column number containing the error.</summary>
        public int Column { get; private set; } = 0;

        public ImageMoniker Moniker => _error.Moniker;

        public bool IsSupported => _error.IsSupported;

        /// <summary>Register the error on the specified ParseItem.</summary>
        public void Register(params string[] tokens)
        {
            Register(_item, tokens);
        }

        /// <summary>Register the error on the specified ParseItem.</summary>
        public void Register(ParseItem item, params string[] tokens)
        {
            var span = new SnapshotSpan(item.Document.TextBuffer.CurrentSnapshot, item.Span);
            ITextSnapshotLine line = span.Snapshot.GetLineFromPosition(span.Start);

            Line = line.LineNumber;
            Column = span.Start.Position - line.Start.Position;
            Description = string.Format(_error.DescriptionFormat, tokens);

            item.AddError(this);
        }
    }
}
