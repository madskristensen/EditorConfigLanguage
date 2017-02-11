using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    /// <summary>A section is a globbing pattern matching one or more files.</summary>
    public class Section
    {
        public Section(ParseItem section)
        {
            Item = section;
            Properties = new List<Property>();
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
                Property last = Properties.LastOrDefault();
                return last != null ? new Span(Item.Span.Start, last.Span.End - Item.Span.Start) : Item.Span;
            }
        }
    }
}
