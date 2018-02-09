using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;
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

            Visibility = System.Windows.Visibility.Hidden;

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

        private async void InheritanceUpdated(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            CreateImage();
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

            EditorConfigDocument parent = _document.Parent;
            int parentsCount = 0;

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
                CreateInheritance(parent.FileName, parentsCount);
                parentsCount += 3;
                parent = parent.Parent;
            }

            UpdateLayout();

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            {
                SetAdornmentLocation(_adornmentLayer.TextView, EventArgs.Empty);
            });
        }

        private void CreateInheritance(string parentFileName, int padding)
        {
            string fileName = _adornmentLayer.TextView.TextBuffer.GetFileName();

            if (string.IsNullOrEmpty(fileName))
                return;

            string shortcut = GetShortcut();

            if (!string.IsNullOrEmpty(shortcut))
                ToolTip = $"Navigate to immediate parent ({shortcut})";

            string relative = PackageUtilities.MakeRelative(fileName, parentFileName);

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
                VsHelpers.PreviewDocument(parentFileName);
                Telemetry.TrackUserTask("InheritanceNavigated");
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
                Visibility = System.Windows.Visibility.Visible;
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

        private static string GetShortcut()
        {
            Command cmd = VsHelpers.DTE.Commands.Item("EditorContextMenus.CodeWindow.EditorConfig.NavigateToParent");

            if (cmd == null || !cmd.IsAvailable)
                return null;

            string bindings = ((object[])cmd.Bindings).FirstOrDefault() as string;

            if (!string.IsNullOrEmpty(bindings))
            {
                int index = bindings.IndexOf(':') + 2;
                return bindings.Substring(index);
            }

            return null;
        }

        public static event EventHandler<EventArgs> Updated;
    }
}
;