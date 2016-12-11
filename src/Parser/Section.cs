using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EditorConfig
{
    public class Section
    {
        public Section(ParseItem section)
        {
            Item = section;
            Properties = new List<Property>();
        }

        public ParseItem Item { get; set; }

        public IList<Property> Properties { get; set; }

        public Span Span
        {
            get
            {
                var last = Properties.LastOrDefault();
                return last != null ? Span.FromBounds(Item.Span.Start, last.Span.End) : Item.Span;
            }
        }

        public bool IsValid
        {
            get
            {
                return Properties.All(p => p.IsValid);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (Property property in Properties)
            {
                sb.AppendLine(property.Keyword.Text);

                if (property.Value != null)
                    sb.Append($" = {property.Value.Text}");

                if (property.Severity != null)
                    sb.Append($":{property.Severity.Text}");
            }

            return sb.ToString();
        }
    }
}
