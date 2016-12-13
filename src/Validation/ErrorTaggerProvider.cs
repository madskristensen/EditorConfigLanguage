using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(Constants.LanguageName)]
    [TagType(typeof(ErrorTag))]
    class ErrorTaggerProvider : ITaggerProvider
    {
        [Import]
        ITextDocumentFactoryService DocumentService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer.Properties.TryGetProperty(typeof(ErrorListProvider), out ErrorListProvider errorlist) &&
                buffer.Properties.TryGetProperty(typeof(IWpfTextView), out IWpfTextView view) &&
                DocumentService.TryGetTextDocument(buffer, out var document))
            {
                return buffer.Properties.GetOrCreateSingletonProperty(() => new ErrorTagger(view, errorlist, document)) as ITagger<T>;
            }

            return null;
        }
    }
}
