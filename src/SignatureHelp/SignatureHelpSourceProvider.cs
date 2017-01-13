using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(ISignatureHelpSourceProvider))]
    [Name("EditorConfig section Signature Help Source")]
    [ContentType(Constants.LanguageName)]
    [Order(Before = "default")]
    internal class SectionSignatureHelpSourceProvider : ISignatureHelpSourceProvider
    {
        public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new SectionSignatureHelpSource(textBuffer));
        }
    }
}
