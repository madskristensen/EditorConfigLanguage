using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;

namespace EditorConfig
{
    [Export(typeof(IUIElementProvider<Completion, ICompletionSession>))]
    [Name("EditorConfigCompletionTooltipCustomization")]
    [ContentType(Constants.LanguageName)]
    public class CompletionElementProvider : IUIElementProvider<Completion, ICompletionSession>
    {
        public UIElement GetUIElement(Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (elementType == UIElementType.Tooltip)
            {
                if (SchemaCatalog.TryGetProperty(itemToRender.DisplayText, out Keyword prop))
                    return new Shared.EditorTooltip(prop);

                if (SchemaCatalog.TryGetSeverity(itemToRender.DisplayText, out Severity severity))
                    return new Shared.EditorTooltip(severity);
            }

            return null;
        }
    }
}
