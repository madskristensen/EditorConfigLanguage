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

        internal EditorTooltip(ITooltip item)
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                ItemName.Content = PrettifyName(item);
                ItemName.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

                string description = item.Description;

                if (!item.IsSupported)
                    description += $"\r\n\r\n{EditorConfig.Resources.Text.NotSupportedByVS}";

                Description.Text = description;
                Description.SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);

                Glyph.Source = item.Moniker.ToBitmap(_iconSize);
            };
        }

        private static string PrettifyName(ITooltip item)
        {
            string text = item.Name
                           .Replace("_", " ")
                           .Replace("dotnet", ".NET")
                           .Replace("csharp", "C#");

            if (text.Length > 0)
                text = text[0].ToString().ToUpperInvariant() + text.Substring(1);

            return text;
        }
    }
}
