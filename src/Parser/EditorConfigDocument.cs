using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    /// <summary>A representation of the .editorconfig document.</summary>
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
        public List<ParseItem> ParseItems { get; private set; } = new List<ParseItem>();

        /// <summary>A list of all the sections in the document.</summary>
        public List<Section> Sections { get; private set; } = new List<Section>();

        /// <summary>A list of all the properties in the root of the document.</summary>
        public List<Property> Properties { get; private set; } = new List<Property>();

        /// <summary>A list of all the error suppressions in the document.</summary>
        public List<string> Suppressions { get; private set; } = new List<string>();

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
            foreach (Property property in Properties)
            {
                if (property.Span.Contains(position - 1))
                    return property;
            }

            foreach (Property property in Sections.SelectMany(s => s.Properties))
            {
                if (property.Span.Contains(position - 1))
                    return property;
            }

            return null;
        }

        /// <summary>Returns a list of all rules included in the current or parent document(s).</summary>
        public List<string> GetAllIncludedRules()
        {
            EditorConfigDocument curDoc = this;
            var rules = new List<string>();
            while (curDoc != null)
            {
                foreach (ParseItem parseItem in curDoc.ParseItems)
                {
                    string parseItemStr = parseItem.Text;
                    if (!rules.Contains(parseItemStr) && parseItem.ItemType == ItemType.Keyword)
                    {
                        rules.Add(parseItemStr);
                    }
                }
                curDoc = curDoc.Parent;
            }
            return rules;
        }

        public void Dispose()
        {
            DisposeParser();
            DisposeInheritance();
        }
    }
}
