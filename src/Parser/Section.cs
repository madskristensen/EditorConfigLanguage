using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace EditorConfig
{
    /// <summary>A section is a globbing pattern matching one or more files.</summary>
    public class Section
    {
        public Section(ParseItem section)
        {
            Item = section;
            Properties = [];
        }

        /// <summary>The ParseItem containing the section display text.</summary>
        public ParseItem Item { get; }

        /// <summary>A list of properties under the Section.</summary>
        public IList<Property> Properties { get; }

        /// <summary>The full span of the section including the properties.</summary>
        public Span Span
        {
            get
            {
                // Access last element directly via indexer for O(1) instead of LINQ LastOrDefault O(n)
                if (Properties.Count == 0)
                    return Item.Span;

                Property last = Properties[Properties.Count - 1];
                return new Span(Item.Span.Start, last.Span.End - Item.Span.Start);
            }
        }
    }
}
