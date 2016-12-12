using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(Constants.LanguageName)]
    [Name("Editor Config")]
    public class EditorConfigCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigCompletionSource(textBuffer, NavigatorService));
        }
    }
}
