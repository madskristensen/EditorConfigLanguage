using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("EditorConfig QuickInfo Controller")]
    [ContentType(Constants.LanguageName)]
    public class EditorConfigQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        public IAsyncQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            // Check if package is loaded; if not, default to enabling QuickInfo
            bool enableQuickInfo = EditorConfigPackage.Language?.Preferences?.EnableQuickInfo ?? true;

            if (enableQuickInfo && subjectBuffers.Count > 0)
            {
                return textView.Properties.GetOrCreateSingletonProperty(() => new EditorConfigQuickInfoController(textView, subjectBuffers, this));
            }

            return null;
        }
    }
}
