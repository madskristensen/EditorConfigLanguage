using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EditorConfig
{
    internal sealed class SeverityAdornment : Image
    {
        private const int _size = 16;

        internal SeverityAdornment(SeverityTag tag)
        {
            Loaded += (s, e) =>
            {
                Height = _size;
                Width = _size;
                Margin = new Thickness(8, 0, 0, 0);
                Cursor = Cursors.Arrow;

                Update(tag);
            };
        }

        internal void Update(SeverityTag tag)
        {
            if (tag == null)
                return;

            string text = tag.ParseItem.Text.ToLowerInvariant();

            if (Constants.Severities.ContainsKey(text))
            {
                var moniker = Constants.Severities[text];
                Source = GetImage(moniker, _size);
                ToolTip = $"Severity: {text}";
            }
        }

        private static BitmapSource GetImage(ImageMoniker moniker, int size)
        {
            ImageAttributes imageAttributes = new ImageAttributes()
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                Dpi = 96,
                LogicalHeight = size,
                LogicalWidth = size,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            var service = (IVsImageService2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsImageService));
            IVsUIObject result = service.GetImage(moniker, imageAttributes);
            result.get_Data(out object data);

            return data as BitmapSource;
        }
    }

}
