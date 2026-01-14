using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name("Editor Config")]
    public class EditorConfigCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigCompletionSource(textBuffer));
        }
    }
}
