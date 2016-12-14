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
        public const string DefaultFileContent = "[*]\r\nend_of_line = crlf\r\n\r\n[*.xml]\r\nindent_style = space";
    }
}
