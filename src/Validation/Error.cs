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
            ITextSnapshotLine line = span.Snapshot.GetLineFromPosition(span.Start);

            Line = line.LineNumber;
            Column = span.Start.Position - line.Start.Position;
        }

        /// <summary>The error code is displayed in the Error List.</summary>
        public string ErrorCode { get; set; }

        /// <summary>The error type determines how to display the error in the Error List.</summary>
        public ErrorType ErrorType { get; set; } = ErrorType.Warning;

        /// <summary>A clear description of the error.</summary>
        public string Description { get; set; }

        /// <summary>The line number containing the error.</summary>
        public int Line { get; }

        /// <summary>The column number containing the error.</summary>
        public int Column { get; }
        public string HelpLink => string.Format(Constants.HelpLink, ErrorCode?.ToLowerInvariant());

        /// <summary>The image moniker that represents the error.</summary>
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

        /// <summary>The name of the error which is always the error type.</summary>
        public string Name
        {
            get
            {
                return ErrorType.ToString();
            }
        }

        /// <summary>Always returns true.</summary>
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
