using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using System.Windows;
using System.Windows.Controls;

namespace EditorConfig
{
    public class TooltipControl : StackPanel
    {
        private const int _iconSize = 14;

        internal TooltipControl(ISchemaItem item) : this(item.Name, item.Description, item.Moniker)
        { }

        //internal TooltipControl(ParseItem item) : this(Property.FromName(item.Text))
        //{ }

        //internal TooltipControl(Property item) : this(item?.Name, item?.Description, item.Moniker)
        //{ }

        internal TooltipControl(string name, string description, ImageMoniker moniker)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
                return;

            Loaded += (s, e) =>
            {
                var nameControl = CreateNameControl(name, moniker);
                var desciptionControl = CreateDescriptionControl(description);

                Children.Add(nameControl);
                Children.Add(desciptionControl);
            };
        }

        private static UIElement CreateDescriptionControl(string description)
        {
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
