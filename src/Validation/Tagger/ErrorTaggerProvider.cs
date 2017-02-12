using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(ErrorTag))]
    class ErrorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer.Properties.TryGetProperty(typeof(IWpfTextView), out IWpfTextView view))
            {
                return buffer.Properties.GetOrCreateSingletonProperty(() => new ErrorTagger(view)) as ITagger<T>;
            }

            return null;
        }
    }
}