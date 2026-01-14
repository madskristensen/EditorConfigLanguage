using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    class SortPropertiesAction(Section section, ITextView view) : BaseSuggestedAction
    {
        public override string DisplayText
        {
            get { return "Sort Properties in Section"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.SortAscending; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotPoint caretPost = view.Caret.Position.BufferPosition;

            using (ITextEdit edit = view.TextBuffer.CreateEdit())
            {
                SortSection(section, edit);

                if (edit.HasEffectiveChanges)
                    edit.Apply();
            }

            view.Caret.MoveTo(new SnapshotPoint(view.TextBuffer.CurrentSnapshot, caretPost));
        }

        public static void SortSection(Section section, ITextEdit edit)
        {
            Property first = section.Properties.FirstOrDefault();
            Property last = section.Properties.LastOrDefault();

            if (first == null)
                return;

            // Get lines starting AFTER the section header to include any comments before the first property
            int sectionHeaderEnd = section.Item.Span.End;
            IEnumerable<ITextSnapshotLine> bufferLines = edit.Snapshot.Lines.Where(l => l.Start > sectionHeaderEnd && l.End <= last.Span.End);
            List<string> lines = [.. bufferLines.Select(b => b.GetText())];

            // Parse into blocks (separated by empty lines)
            // Each block contains sub-groups (a comment starts a new sub-group)
            // Sub-groups that start with a comment are kept in original order (comment = group header)
            // Sub-groups without a leading comment have their properties sorted
            var blocks = new List<Block>();
            var currentBlock = new Block();
            var currentSubGroup = new SubGroup();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line marks the end of a block
                    currentBlock.FinalizeSubGroup(currentSubGroup);
                    if (!currentBlock.IsEmpty)
                    {
                        blocks.Add(currentBlock);
                    }
                    currentBlock = new Block();
                    currentSubGroup = new SubGroup();
                    continue;
                }

                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                {
                    // Comment starts a new sub-group (finalize current one first if it has properties)
                    if (currentSubGroup.HasProperties)
                    {
                        currentBlock.FinalizeSubGroup(currentSubGroup);
                        currentSubGroup = new SubGroup();
                    }
                    currentSubGroup.AddComment(line);
                }
                else
                {
                    // Property line
                    currentSubGroup.AddProperty(line);
                }
            }

            // Add the last block
            currentBlock.FinalizeSubGroup(currentSubGroup);
            if (!currentBlock.IsEmpty)
            {
                blocks.Add(currentBlock);
            }

            // Build output
            var sb = new StringBuilder();
            sb.AppendLine(section.Item.Text);

            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].AppendTo(sb);

                // Add empty line between blocks (but not after the last one)
                if (i < blocks.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            edit.Replace(section.Span, sb.ToString().TrimEnd());
        }

        /// <summary>A block is a group of lines separated by empty lines.</summary>
        private class Block
        {
            private readonly List<SubGroup> _subGroups = [];

            public bool IsEmpty => _subGroups.Count == 0;

            public void FinalizeSubGroup(SubGroup subGroup)
            {
                if (!subGroup.IsEmpty)
                {
                    _subGroups.Add(subGroup);
                }
            }

            public void AppendTo(StringBuilder sb)
            {
                foreach (SubGroup subGroup in _subGroups)
                {
                    subGroup.AppendTo(sb);
                }
            }
        }

        /// <summary>A sub-group is a set of comments followed by properties.</summary>
        private class SubGroup
        {
            private readonly List<string> _comments = [];
            private readonly List<string> _properties = [];

            public bool IsEmpty => _comments.Count == 0 && _properties.Count == 0;
            public bool HasProperties => _properties.Count > 0;

            public void AddComment(string line) => _comments.Add(line);
            public void AddProperty(string line) => _properties.Add(line);

            public void AppendTo(StringBuilder sb)
            {
                // Output comments first (they act as a group header)
                foreach (string comment in _comments)
                {
                    sb.AppendLine(comment);
                }

                // Always sort properties within the sub-group
                IEnumerable<string> sortedProperties = _properties
                    .OrderBy(p => p.TrimStart().IndexOf("csharp_") + p.TrimStart().IndexOf("dotnet_"))
                    .ThenBy(p => p.TrimStart());

                foreach (string property in sortedProperties)
                {
                    sb.AppendLine(property);
                }
            }
        }
    }
}
