using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Text;
using System.Collections.Specialized;

namespace EditorConfig
{
    internal class EditorConfigDropHandler : IDropHandler
    {
        private IWpfTextView _view;
        private ITextBufferUndoManager _undoManager;
        private string _ext;

        const string _template = "[*{0}]";

        public EditorConfigDropHandler(IWpfTextView view, ITextBufferUndoManager undoManager)
        {
            _view = view;
            _undoManager = undoManager;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            try
            {

                SnapshotPoint position = dragDropInfo.VirtualBufferPosition.Position;
                string header = string.Format(_template, _ext);

                ITextSnapshotLine line = _view.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);

                if (!line.Extent.IsEmpty)
                    header = Environment.NewLine + header;

                using (ITextUndoTransaction transaction = _undoManager.TextBufferUndoHistory.CreateTransaction($"Dragged {_ext}"))
                using (ITextEdit edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Insert(position, header);
                    edit.Apply();
                    transaction.Complete();
                }

                Telemetry.TrackUserTask("FileDragged");
            }
            catch (Exception ex)
            {
                Telemetry.TrackException("DragDrop", ex);
            }

            return DragDropPointerEffects.Copy;
        }

        public void HandleDragCanceled()
        { }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            string draggedFileName = GetDraggedFilename(dragDropInfo);
            _ext = Path.GetExtension(draggedFileName);

            return !string.IsNullOrWhiteSpace(_ext);
        }

        private static string GetDraggedFilename(DragDropInfo info)
        {
            var data = new DataObject(info.Data);

            if (info.Data.GetDataPresent("FileDrop"))
            {
                // The drag and drop operation came from the file system
                StringCollection files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                {
                    return files[0];
                }
            }
            else if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                // The drag and drop operation came from the VS solution explorer
                return data.GetText();
            }

            return null;
        }
    }
}