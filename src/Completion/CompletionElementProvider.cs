using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;

namespace EditorConfig
{
    [Export(typeof(IUIElementProvider<Completion, ICompletionSession>))]
    [Name("EditorConfigCompletionTooltipCustomization")]
    [ContentType(Constants.LanguageName)]
    public class CompletionElementProvider : IUIElementProvider<Completion, ICompletionSession>
    {
        public UIElement GetUIElement(Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (elementType == UIElementType.Tooltip &&
                itemToRender.Properties.TryGetProperty("item", out ITooltip item) &&
                !string.IsNullOrEmpty(item.Description))
            {
                return new Shared.EditorTooltip(item);
            }

            return null;
        }
    }
}
