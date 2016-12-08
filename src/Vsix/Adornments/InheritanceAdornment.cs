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
    class InheritanceAdornment
    {
        private IAdornmentLayer _adornmentLayer;
        private Panel _adornment = new StackPanel();
        private EditorConfigDocument _document;
        private ITextDocumentFactoryService _documentService;

        public InheritanceAdornment(IWpfTextView view, ITextDocumentFactoryService documentService)
        {
            if (!documentService.TryGetTextDocument(view.TextBuffer, out ITextDocument doc))
                return;

            _documentService = documentService;
            _adornmentLayer = view.GetAdornmentLayer(InheritanceAdornmentLayer.LayerName);
            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);

            CreateImage();

            doc.FileActionOccurred += FileActionOccurred;

            view.ViewportHeightChanged += SetAdornmentLocation;
            view.ViewportWidthChanged += SetAdornmentLocation;

            if (_adornmentLayer.IsEmpty)
                _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _adornment, null);
        }

        private void FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                CreateImage();
            }
        }

        private void CreateImage()
        {
            _adornment.Children.Clear();

            var parent = _document.InheritsFrom(out string parentFileName);
            var parentsCount = 0;

            if (parent != null)
            {
                var inherits = new TextBlock()
                {
                    Text = "Inherits from",
                    FontSize = 22,
                    Foreground = Brushes.Gray,
                    Opacity = 0.5,
                };

                inherits.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased);
                inherits.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
                _adornment.Children.Add(inherits);
            }

            while (parent != null)
            {
                CreateInheritance(parentFileName, parentsCount);
                parentsCount += 4;
                parent = parent.InheritsFrom(out parentFileName);
            }

            _adornment.UpdateLayout();

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

            var inherits = new TextBlock()
            {
                Text = ("└─  " + relative).PadLeft(relative.Length + 4 + padding),// "→ " + relative, 
                FontSize = 16,
                Foreground = Brushes.Gray,
                Opacity = 0.5,
                Cursor = Cursors.Hand,
                ToolTip = "Click to open " + parentFileName,
                Margin = new System.Windows.Thickness(0, 3, 0, 0),
            };

            inherits.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased);
            inherits.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
            inherits.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                VsHelpers.DTE.ItemOperations.OpenFile(parentFileName);
            };

            _adornment.Children.Add(inherits);
        }

        private void SetAdornmentLocation(object sender, EventArgs e)
        {
            if (_adornment.ActualWidth == 0)
            {
                Canvas.SetLeft(_adornment, 9999);
                Canvas.SetTop(_adornment, 9999);
            }
            else
            {
                IWpfTextView view = _adornmentLayer.TextView;
                Canvas.SetLeft(_adornment, view.ViewportRight - _adornment.ActualWidth - 20);
                Canvas.SetTop(_adornment, view.ViewportBottom - _adornment.ActualHeight - 20);
            }
        }
    }
}
;