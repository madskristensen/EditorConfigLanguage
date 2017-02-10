using System;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public class ErrorCode : ITooltip
    {
        private string _message;

        public ErrorCode(string code, ErrorCategory type, string message)
        {
            Code = code;
            Category = type;
            _message = message;
        }

        public string Code { get; }
        public ErrorCategory Category { get; }

        public void Register(ParseItem item, params string[] tokens)
        {
            if (item.Document.Suppressions.Contains(Code, StringComparer.OrdinalIgnoreCase))
                return;

            string description = string.Format(_message, tokens);

            var error = new Error(item, Code, Category, description);
            item.AddError(error);
        }

        string ITooltip.Name => Code;
        string ITooltip.Description => string.Format(_message, "x", "y", "z", "a", "b", "c");
        ImageMoniker ITooltip.Moniker => KnownMonikers.ValidationRule;
        bool ITooltip.IsSupported => true;
    }
}
