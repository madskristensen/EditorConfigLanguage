using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace EditorConfig
{
    internal sealed class RenameNamingEntity : BaseCommand
    {
        private const uint _commandId = (uint)VSConstants.VSStd2KCmdID.ECMD_RENAMESYMBOL;
        private readonly IWpfTextView _view;
        private readonly ITextBufferUndoManager _undoManager;

        internal RenameNamingEntity(IWpfTextView view, ITextBufferUndoManager undoManager)
        {
            _view = view;
            _undoManager = undoManager;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup != VSConstants.VSStd2K || nCmdID != _commandId ||
                !TryGetSymbol(out EditorConfigDocument document, out NamingSymbol symbol))
            {
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            var dialog = new RenameNamingEntityDialog(symbol.Name);
            while (dialog.ShowModal() == true)
            {
                string newName = dialog.EntityName;
                if (NamingSymbolService.TryGetRenameSpans(document, symbol, newName, out IReadOnlyList<Span> spans, out string error))
                {
                    Rename(spans, newName);
                    return VSConstants.S_OK;
                }

                dialog = new RenameNamingEntityDialog(newName, error);
            }

            return VSConstants.S_OK;
        }

        public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == VSConstants.VSStd2K && prgCmds[0].cmdID == _commandId && TryGetSymbol(out _, out _))
            {
                prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                return VSConstants.S_OK;
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private bool TryGetSymbol(out EditorConfigDocument document, out NamingSymbol symbol)
        {
            document = EditorConfigDocument.FromTextBuffer(_view.TextBuffer);
            int position = _view.Caret.Position.BufferPosition.Position;
            return NamingSymbolService.TryGetSymbolAtPosition(document, position, out symbol) &&
                   NamingSymbolService.TryGetDefinition(document, symbol, out _);
        }

        private void Rename(IReadOnlyList<Span> spans, string newName)
        {
            using (ITextUndoTransaction transaction = _undoManager.TextBufferUndoHistory.CreateTransaction("Rename naming entity"))
            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                foreach (Span span in spans)
                {
                    edit.Replace(span, newName);
                }

                if (edit.Apply() != null)
                    transaction.Complete();
                else
                    transaction.Cancel();
            }
        }
    }

    internal sealed class RenameNamingEntityDialog : DialogWindow
    {
        private readonly TextBox _name;

        internal RenameNamingEntityDialog(string currentName, string validationMessage = null)
        {
            Title = "Rename naming entity";
            Width = 420;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "New name:", Margin = new Thickness(0, 0, 0, 4) });

            _name = new TextBox { Text = currentName };
            panel.Children.Add(_name);

            if (validationMessage != null)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = validationMessage,
                    Margin = new Thickness(0, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                });
            }

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0),
            };

            var rename = new Button { Content = "Rename", IsDefault = true, MinWidth = 80 };
            rename.Click += (_, _) => DialogResult = true;
            buttons.Children.Add(rename);

            var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);
            Content = panel;

            Loaded += (_, _) =>
            {
                _name.Focus();
                _name.SelectAll();
            };
        }

        internal string EntityName => _name.Text;
    }
}
