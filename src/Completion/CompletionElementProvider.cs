using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
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
        [Import]
        private IViewElementFactoryService ViewElementFactoryService { get; set; }

        public UIElement GetUIElement(Completion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (elementType == UIElementType.Tooltip &&
                itemToRender.Properties.TryGetProperty("item", out ITooltip item) &&
                !string.IsNullOrEmpty(item.Description))
            {
                ContainerElement tooltip = QuickInfoBuilder.BuildTooltip(item);
                ITextView textView = context.TextView;
                UIElement element = ViewElementFactoryService.CreateViewElement<UIElement>(textView, tooltip);

                return new Border
                {
                    Child = element,
                    Padding = new Thickness(6)
                };
            }

            return null;
        }
    }
}
