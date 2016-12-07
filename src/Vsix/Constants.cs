using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;

namespace EditorConfig
{
    public class Constants
    {
        public const string LanguageName = "EditorConfig";
        public const string FileName = ".editorconfig";
        public const string Homepage = "http://editorconfig.org";
        
        public static Dictionary<string, ImageMoniker> Severities = new Dictionary<string, ImageMoniker>
        {
            { "none", KnownMonikers.None},
            { "suggestion", KnownMonikers.StatusInformation},
            { "warning", KnownMonikers.StatusWarning},
            { "error", KnownMonikers.StatusError},
        };
    }
}
