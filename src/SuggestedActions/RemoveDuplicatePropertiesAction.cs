using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace EditorConfig
{
    class RemoveDuplicatePropertiesAction : BaseSuggestedAction
    {
        private readonly Section _section;
        private readonly ITextView _view;

        // Cache the error codes to check - avoids repeated property access
        private static readonly HashSet<string> _duplicateErrorCodes = new(System.StringComparer.Ordinal)
        {
            ErrorCatalog.DuplicateProperty.Code,
            ErrorCatalog.ParentDuplicateProperty.Code
        };

        public RemoveDuplicatePropertiesAction(Section section, ITextView view)
        {
            _section = section;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Remove duplicates"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.CleanData; }
        }

        public override bool IsEnabled
        {
            get
            {
                // Avoid nested Any() - use explicit loop with early exit
                foreach (Property p in _section.Properties)
                {
                    foreach (DisplayError e in p.Keyword.Errors)
                    {
                        if (_duplicateErrorCodes.Contains(e.Name))
                            return true;
                    }
                }
                return false;
            }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = _view.Caret.Position.BufferPosition;
            
            // Avoid nested Any() - collect duplicates with explicit loop
            var duplicates = new List<Property>();
            foreach (Property p in _section.Properties)
            {
                foreach (DisplayError e in p.Keyword.Errors)
                {
                    if (_duplicateErrorCodes.Contains(e.Name))
                    {
                        duplicates.Add(p);
                        break; // Found a match, no need to check other errors
                    }
                }
            }

            using (ITextEdit edit = _view.TextBuffer.CreateEdit())
            {
                // Process in reverse to maintain valid positions
                for (int i = duplicates.Count - 1; i >= 0; i--)
                {
                    Property dupe = duplicates[i];
                    ITextSnapshotLine line = _view.TextBuffer.CurrentSnapshot.GetLineFromPosition(dupe.Span.Start);
                    edit.Delete(line.ExtentIncludingLineBreak);
                }

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            _view.Caret.MoveTo(new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, caretPost));
        }
    }
}
