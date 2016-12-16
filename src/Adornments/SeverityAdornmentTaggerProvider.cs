using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal sealed class SeverityAdornmentTaggerProvider : IViewTaggerProvider
    {
        [Import]
        private IBufferTagAggregatorFactoryService TagAggregatorService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            var lazy = new Lazy<ITagAggregator<SeverityTag>>(() => TagAggregatorService.CreateTagAggregator<SeverityTag>(textView.TextBuffer));

            return SeverityAdornmentTagger.GetTagger((IWpfTextView)textView, lazy) as ITagger<T>;
        }
    }
}