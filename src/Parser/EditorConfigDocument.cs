using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    sealed partial class EditorConfigDocument : IDisposable
    {
        private EditorConfigDocument(ITextBuffer buffer)
        {
            TextBuffer = buffer;

            InitializeParser();
            InitializeInheritance();
        }

        /// <summary>The ITextBuffer associated with the document.</summary>
        public ITextBuffer TextBuffer { get; }

        /// <summary>A list of all the parse items in the document.</summary>
        public List<ParseItem> ParseItems { get; } = new List<ParseItem>();

        /// <summary>A list of all the sections in the document.</summary>
        public List<Section> Sections { get; } = new List<Section>();

        /// <summary>A list of all the properties in the root of the document.</summary>
        public List<Property> Properties { get; } = new List<Property>();

        /// <summary>The root property of the document if one is specified</summary>
        public Property Root
        {
            get
            {
                return Properties.FirstOrDefault(p => p.Keyword.Text.Equals(SchemaCatalog.Root));
            }
        }

        /// <summary>A list of all the sections in the document.</summary>
        public static EditorConfigDocument FromTextBuffer(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new EditorConfigDocument(buffer));
        }

        /// <summary>Returns all the parse items contained within the specified span.</summary>
        public IEnumerable<ParseItem> ItemsInSpan(Span span)
        {
            return ParseItems?.Where(i => span.Contains(i.Span));
        }

        /// <summary>Returns the ParseItem located at the specified position.</summary>
        public ParseItem ItemAtPosition(int position)
        {
            return ParseItems?.FirstOrDefault(p => p.Span.Contains(position - 1));
        }

        /// <summary>Returns the Property located at the specified position.</summary>
        public Property PropertyAtPosition(int position)
        {
            foreach (var property in Properties)
            {
                if (property.Span.Contains(position - 1))
                    return property;
            }

            foreach (var property in Sections.SelectMany(s => s.Properties))
            {
                if (property.Span.Contains(position - 1))
                    return property;
            }

            return null;
        }

        public void Dispose()
        {
            DisposeParser();
            DisposeInheritance();
        }
    }
}
