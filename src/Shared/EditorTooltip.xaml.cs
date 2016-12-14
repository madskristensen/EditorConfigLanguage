using Microsoft.VisualStudio.PlatformUI;
using System.Windows.Controls;

namespace EditorConfig.Shared
{
    /// <summary>
    /// Interaction logic for EditorTooltip.xaml
    /// </summary>
    public partial class EditorTooltip : UserControl
    {
        private const int _iconSize = 32;

        internal EditorTooltip(ISchemaItem item)
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                ItemName.Content = item.Name;
                ItemName.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

                var description = item.Description;

                if (!item.IsSupported)
                    description += $"\r\n\r\n{EditorConfig.Resources.Text.NotSupportedByVS}";

                Description.Text = description;
                Description.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

                Glyph.Source = item.Moniker.ToBitmap(_iconSize);
            };
        }
    }
}
