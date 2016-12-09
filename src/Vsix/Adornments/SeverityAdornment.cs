using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EditorConfig
{
    internal sealed class SeverityAdornment : Image
    {
        private const int _size = 14;
        private static Dictionary<string, BitmapSource> _imageCache = new Dictionary<string, BitmapSource>();

        internal SeverityAdornment(SeverityTag tag)
        {
            Loaded += (s, e) =>
            {
                Height = _size;
                Width = _size;
                Margin = new Thickness(8, 0, 0, 0);
                Cursor = Cursors.Arrow;
                MouseLeftButtonUp += Clicked;

                Update(tag);
            };
        }

        private void Clicked(object sender, MouseButtonEventArgs e)
        {
            VsHelpers.DTE.ExecuteCommandSafe("Edit.ListMembers");
        }

        internal void Update(SeverityTag tag)
        {
            if (tag == null)
                return;

            string text = tag.ParseItem.Text.ToLowerInvariant();

            if (Constants.Severities.ContainsKey(text))
            {
                Source = GetBitmapSource(text);
                ToolTip = $"Severity: {text}";
            }
        }

        private static BitmapSource GetBitmapSource(string severity)
        {
            if (!_imageCache.ContainsKey(severity))
            {
                var moniker = Constants.Severities[severity];
                var bitmap = GetImage(moniker, _size);
                _imageCache.Add(severity, bitmap);
            }

            return _imageCache[severity];
        }

        private static BitmapSource GetImage(ImageMoniker moniker, int size)
        {
            var shell = (IVsUIShell5)Package.GetGlobalService(typeof(SVsUIShell));
            var backgroundColor = VsColors.GetThemedColorRgba(shell, EnvironmentColors.MainWindowButtonInactiveBorderBrushKey);

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags | unchecked((uint)_ImageAttributesFlags.IAF_Background),
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                Dpi = 96,
                LogicalHeight = size,
                LogicalWidth = size,
                Background = backgroundColor,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            var service = (IVsImageService2)Package.GetGlobalService(typeof(SVsImageService));
            IVsUIObject result = service.GetImage(moniker, imageAttributes);
            result.get_Data(out object data);

            return data as BitmapSource;
        }
    }
}
