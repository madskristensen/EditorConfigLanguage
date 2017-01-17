using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    public class Error : ITooltip
    {
        public Error(ParseItem item, string errorCode, ErrorType errorType, string description)
        {
            ErrorCode = errorCode;
            ErrorType = errorType;
            Description = description;

            var span = new SnapshotSpan(item.Document.TextBuffer.CurrentSnapshot, item.Span);
            var line = span.Snapshot.GetLineFromPosition(span.Start);

            Line = line.LineNumber;
            Column = span.Start.Position - line.Start.Position;
        }

        public string ErrorCode { get; set; }
        public ErrorType ErrorType { get; set; } = ErrorType.Warning;
        public string Description { get; set; }
        public int Line { get; }
        public int Column { get; }

        public ImageMoniker Moniker
        {
            get
            {
                switch (ErrorType)
                {
                    case ErrorType.Error:
                        return KnownMonikers.StatusError;
                    case ErrorType.Warning:
                        return KnownMonikers.StatusWarning;
                }

                return KnownMonikers.StatusInformation;
            }
        }

        public string Name
        {
            get
            {
                return ErrorType.ToString();
            }
        }

        public bool IsSupported => true;

        public override int GetHashCode()
        {
            return ErrorCode.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Error other))
                return false;

            return Equals(other);
        }

        public bool Equals(Error other)
        {
            if (other == null)
                return false;

            return string.Equals(ErrorCode, other.ErrorCode, System.StringComparison.Ordinal);
        }
    }
}
