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
                if (SchemaCatalog.TryGetProperty(itemToRender.DisplayText, out Property prop))
                    return new TooltipControl(prop);

                if (SchemaCatalog.TryGetSeverity(itemToRender.DisplayText, out Severity severity))
                    return new TooltipControl(severity);
            }

            return null;
        }
    }
}
