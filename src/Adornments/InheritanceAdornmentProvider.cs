using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    class AdornmentProvider : IWpfTextViewCreationListener
    {
        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (DocumentService.TryGetTextDocument(textView.TextBuffer, out ITextDocument doc))
            {
                textView.Properties.GetOrCreateSingletonProperty(() => new InheritanceAdornment(textView, doc));
            }
        }
    }

    class InheritanceAdornmentLayer
    {
        public const string LayerName = "EditorConfig Inheritance Layer";

        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(Before = PredefinedAdornmentLayers.Caret)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;
    }
}