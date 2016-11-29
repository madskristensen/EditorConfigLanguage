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
        public IQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            if (EditorConfigPackage.Language.Preferences.EnableQuickInfo && subjectBuffers.Count > 0)
            {
                return textView.Properties.GetOrCreateSingletonProperty(() => new EditorConfigQuickInfoController(textView, subjectBuffers, this));
            }

            return null;
        }
    }
}
