using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Editor Config Suggested Actions")]
    [ContentType(Constants.LanguageName)]
    class SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer buffer)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsSource(textView, buffer));
        }
    }
}
