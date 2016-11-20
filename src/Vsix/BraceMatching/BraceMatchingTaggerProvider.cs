using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypes.EditorConfig)]
    [TagType(typeof(TextMarkerTag))]
    class BraceMatchingTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (EditorConfigLanguage.preferences == null || !EditorConfigLanguage.preferences.EnableShowMatchingBrace)
                return null;

            //provide highlighting only on the top-level buffer
            if (textView.TextBuffer != buffer)
                return null;

            return new BraceMatchingTagger(textView, buffer) as ITagger<T>;
        }
    }
}
