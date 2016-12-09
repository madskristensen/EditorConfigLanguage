using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(Constants.LanguageName)]
    internal class EditorConfigProvider : IClassifierProvider
    {
        [Import]
        private IClassificationTypeRegistryService ClassificationRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigClassifier(ClassificationRegistry, buffer));
        }
    }
}
