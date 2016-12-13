using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        private EditorConfigDocument(ITextBuffer buffer)
        {
            TextBuffer = buffer;
            TextBuffer.Changed += BufferChangedAsync;

            InitializeParser();
            InitializeInheritance();
        }

        private async void BufferChangedAsync(object sender, EventArgs e)
        {
            await ParseAsync();
        }

        public ITextBuffer TextBuffer { get; }

        public List<ParseItem> ParseItems { get; } = new List<ParseItem>();

        public List<Section> Sections { get; } = new List<Section>();

        public List<Property> Properties { get; } = new List<Property>();

        public Property Root
        {
            get
            {
                return Properties.FirstOrDefault(p => p.Keyword.Text.Equals(SchemaCatalog.Root));
            }
        }

        public static EditorConfigDocument FromTextBuffer(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigDocument(buffer));
        }

        public IEnumerable<ParseItem> ItemsInSpan(Span span)
        {
            return ParseItems?.Where(i => span.Contains(i.Span));
        }

        public ParseItem ItemAtPosition(int point)
        {
            return ParseItems?.FirstOrDefault(p => p.Span.Contains(point - 1));
        }
    }
}
