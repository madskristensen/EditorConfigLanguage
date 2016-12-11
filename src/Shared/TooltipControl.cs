using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Controls;

namespace EditorConfig
{
    public class TooltipControl : StackPanel
    {
        private const int _iconSize = 14;

        internal TooltipControl(ISchemaItem item)
        {
            Loaded += (s, e) =>
            {
                var nameControl = CreateNameControl(item.Name, item.Moniker);
                var desciptionControl = CreateDescriptionControl(item.Description, item.IsSupported);

                Children.Add(nameControl);
                Children.Add(desciptionControl);
            };
        }

        private static UIElement CreateDescriptionControl(string description, bool isSupported)
        {
            if (!isSupported)
                description += $"\r\n\r\n{EditorConfig.Resources.Text.NotSupportedByVS}";

            var descBlock = new TextBlock()
            {
                Text = description,
                Margin = new Thickness(0, 4, 0, 0),
                MaxWidth = 500,
                TextWrapping = TextWrapping.Wrap,
            };
            descBlock.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);
            return descBlock;
        }

        private UIElement CreateNameControl(string name, ImageMoniker moniker)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };

            var image = new Image
            {
                Source = moniker.ToBitmap(_iconSize),
                Margin = new Thickness(0, 0, 5, 0),
            };

            var text = new TextBlock
            {
                Text = name,
                FontWeight = FontWeights.Bold,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

            panel.Children.Add(image);
            panel.Children.Add(text);

            return panel;
        }

    }
}
