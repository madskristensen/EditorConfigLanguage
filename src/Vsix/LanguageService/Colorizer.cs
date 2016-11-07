using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace EditorConfig
{
    public class EditorConfigColorizer : Colorizer
    {
        public EditorConfigColorizer(LanguageService svc, IVsTextLines buffer, IScanner scanner) :
            base(svc, buffer, scanner)
        { }
    }
}
