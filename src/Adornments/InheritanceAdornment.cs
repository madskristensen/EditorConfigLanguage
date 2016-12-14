using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EditorConfig
{
    class InheritanceAdornment : StackPanel
    {
        private IAdornmentLayer _adornmentLayer;
        private EditorConfigDocument _document;

        public InheritanceAdornment(IWpfTextView view, ITextDocument doc)
        {
            _adornmentLayer = view.GetAdornmentLayer(InheritanceAdornmentLayer.LayerName);
            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);

            Loaded += (s, e) =>
            {
                CreateImage();
                doc.FileActionOccurred += FileActionOccurred;
                Updated += InheritanceUpdated;

                view.ViewportHeightChanged += SetAdornmentLocation;
                view.ViewportWidthChanged += SetAdornmentLocation;
            };

            if (_adornmentLayer.IsEmpty)
                _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this, null);
        }

        private void InheritanceUpdated(object sender, EventArgs e)
        {
            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                CreateImage();
            });
        }

        private void FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                Updated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CreateImage()
        {
            Children.Clear();

            var parent = _document.InheritsFrom(out string parentFileName);
            var parentsCount = 0;

            if (parent != null)
            {
                var inherits = new ThemedTextBlock()
                {
                    Text = "Inherits from",
                    FontSize = 22,
                };

                Children.Add(inherits);
            }

            while (parent != null)
            {
                CreateInheritance(parentFileName, parentsCount);
                parentsCount += 3;
                parent = parent.InheritsFrom(out parentFileName);
            }

            UpdateLayout();

            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                SetAdornmentLocation(_adornmentLayer.TextView, EventArgs.Empty);
            });
        }

        private void CreateInheritance(string parentFileName, int padding)
        {
            var fileName = _adornmentLayer.TextView.TextBuffer.GetFileName();

            if (string.IsNullOrEmpty(fileName))
                return;

            var relative = PackageUtilities.MakeRelative(fileName, parentFileName);

            var inherits = new ThemedTextBlock()
            {
                Text = ("└─  " + relative).PadLeft(relative.Length + 4 + padding),// "→ " + relative,
                FontSize = 16,
                Cursor = Cursors.Hand,
                ToolTip = "Click to open " + parentFileName,
                Margin = new System.Windows.Thickness(0, 3, 0, 0),
            };

            inherits.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                VsHelpers.DTE.ItemOperations.OpenFile(parentFileName);
            };

            Children.Add(inherits);
        }

        private void SetAdornmentLocation(object sender, EventArgs e)
        {
            if (ActualWidth == 0)
            {
                Canvas.SetLeft(this, 9999);
                Canvas.SetTop(this, 9999);
            }
            else
            {
                IWpfTextView view = _adornmentLayer.TextView;
                Canvas.SetLeft(this, view.ViewportRight - ActualWidth - 20);
                Canvas.SetTop(this, view.ViewportBottom - ActualHeight - 20);
            }
        }

        private class ThemedTextBlock : TextBlock
        {
            public ThemedTextBlock()
            {
                Opacity = 0.5;
                SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased);
                SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
                SetResourceReference(TextBlock.ForegroundProperty, EnvironmentColors.SystemMenuTextBrushKey);
            }
        }

        public static event EventHandler<EventArgs> Updated;
    }
}
;