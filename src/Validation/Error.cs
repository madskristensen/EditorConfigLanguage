using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Linq;

namespace EditorConfig
{
    public class Error : ITooltip
    {
        private Func<bool> _isSupported;

        public Error(string code, ErrorCategory type, string format, Func<bool> isSupported)
        {
            Code = code;
            Category = type;
            DescriptionFormat = format;
            Description = string.Format(format, "x", "y", "z", "a", "b", "c");
            _isSupported = isSupported;
        }

        /// <summary>The error code is displayed in the Error List.</summary>
        public string Code { get; }

        /// <summary>A short description of the error.</summary>
        public string Description { get; set; }

        public string DescriptionFormat { get; }

        /// <summary>The error category determines how to display the error in the Error List.</summary>
        public ErrorCategory Category { get; }

        string ITooltip.Name => Code;
        public ImageMoniker Moniker => KnownMonikers.ValidationRule;
        public bool IsSupported => _isSupported();

        public void Run(ParseItem item, Action<DisplayError> action)
        {
            Run(item, true, action);
        }

        public void Run(ParseItem item, bool enabled, Action<DisplayError> action)
        {
            if (enabled && item != null && !item.HasErrors && IsSupported && !item.Document.Suppressions.Contains(Code, StringComparer.OrdinalIgnoreCase))
            {
                action.Invoke(new DisplayError(this, item));
            }
        }
    }
}
