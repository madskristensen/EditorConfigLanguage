using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace EditorConfig
{
    internal class EditorConfigSource : Source
    {
        public EditorConfigSource(LanguageService service, IVsTextLines textLines, Colorizer colorizer)
            : base(service, textLines, colorizer)
        { }

        public override CommentInfo GetCommentFormat()
        {
            return new CommentInfo
            {
                UseLineComments = true,
                LineStart = "#"
            };
        }
    }
}

