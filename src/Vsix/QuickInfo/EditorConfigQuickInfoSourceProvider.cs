using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("EditorConfig QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType(ContentTypes.EditorConfig)]
    internal class EditorConfigQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        [Import]
        IGlyphService GlyphService { get; set; }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigQuickInfo(textBuffer, ClassifierAggregatorService, GlyphService));
        }
    }
}
