using System;
using System.Collections.Generic;
using System.Linq;
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
            var severity = SchemaCatalog.Severities.SingleOrDefault(s => s.Name.Equals(text, StringComparison.OrdinalIgnoreCase));
            if (severity != null)
            {
                Source = GetBitmapSource(severity);
            }
        }

        private static BitmapSource GetBitmapSource(Severity severity)
        {
            if (!_imageCache.ContainsKey(severity.Name))
            {
                var bitmap = severity.Moniker.ToBitmap(_size);
                _imageCache.Add(severity.Name, bitmap);
            }

            return _imageCache[severity.Name];
        }
    }
}
