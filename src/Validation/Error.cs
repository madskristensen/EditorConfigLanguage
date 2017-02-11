using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;

namespace EditorConfig
{
    public class Error : ITooltip
    {
        private string _format;

        public Error(string code, ErrorCategory type, string format)
        {
            Code = code;
            Category = type;
            Description = string.Format(format, "x", "y", "z", "a", "b", "c");

            _format = format;
        }

        /// <summary>The error code is displayed in the Error List.</summary>
        public string Code { get; }

        /// <summary>A short description of the error.</summary>
        public string Description { get; set; }

        /// <summary>The error category determines how to display the error in the Error List.</summary>
        public ErrorCategory Category { get; }

        /// <summary>A URL pointing to documentation about the error.</summary>
        public string HelpLink => string.Format(Constants.HelpLink, Code.ToLowerInvariant());

        /// <summary>The line number containing the error.</summary>
        public int Line { get; private set; }

        /// <summary>The column number containing the error.</summary>
        public int Column { get; private set; }

        /// <summary>Register the error on the specified ParseItem.</summary>
        public void Register(ParseItem item, params string[] tokens)
        {
            if (item.Document.Suppressions.Contains(Code, StringComparer.OrdinalIgnoreCase))
                return;

            var span = new SnapshotSpan(item.Document.TextBuffer.CurrentSnapshot, item.Span);
            ITextSnapshotLine line = span.Snapshot.GetLineFromPosition(span.Start);

            Line = line.LineNumber;
            Column = span.Start.Position - line.Start.Position;

            Description = string.Format(_format, tokens);

            item.AddError(this);
        }

        string ITooltip.Name => Code;
        ImageMoniker ITooltip.Moniker => KnownMonikers.ValidationRule;
        bool ITooltip.IsSupported => true;
    }
}
