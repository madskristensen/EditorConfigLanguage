using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Editor Config Suggested Actions")]
    [ContentType(Constants.LanguageName)]
    class SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import]
        IClassifierAggregatorService ClassifierService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer buffer)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsSource(buffer, ClassifierService));
        }
    }
}
