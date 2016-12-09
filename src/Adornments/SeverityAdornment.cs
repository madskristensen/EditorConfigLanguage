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

            if (Constants.SeverityMonikers.ContainsKey(text))
            {
                Source = GetBitmapSource(text);
            }
        }

        private static BitmapSource GetBitmapSource(string severity)
        {
            if (!_imageCache.ContainsKey(severity))
            {
                var moniker = Constants.SeverityMonikers[severity];
                var bitmap = moniker.ToBitmap(_size);
                _imageCache.Add(severity, bitmap);
            }

            return _imageCache[severity];
        }
    }
}
