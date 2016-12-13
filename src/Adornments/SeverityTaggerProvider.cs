using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(SeverityTag))]
    public class SeverityTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //return new SeverityTagger(buffer) as ITagger<T>;
            return buffer.Properties.GetOrCreateSingletonProperty(() => new SeverityTagger(buffer)) as ITagger<T>;
        }
    }
}
