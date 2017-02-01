using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    /// <summary>A property is a keyword/value pair with an optional severity.</summary>
    public class Property
    {
        public Property(ParseItem keyword)
        {
            Keyword = keyword;
        }

        /// <summary>The keyword is the name of the property.</summary>
        public ParseItem Keyword { get; set; }

        /// <summary>The value is what comes after the = character.</summary>
        public ParseItem Value { get; set; }

        /// <summary>This applies to C# and .NET specific keywords only.</summary>
        public ParseItem Severity { get; set; }

        /// <summary>The full span of the property including the value and severity.</summary>
        public Span Span
        {
            get
            {
                ParseItem last = Severity ?? Value ?? Keyword;
                return Span.FromBounds(Keyword.Span.Start, last.Span.End);
            }
        }

        /// <summary>Returns true if there are no syntax errors on the property.</summary>
        public bool IsValid
        {
            get
            {
                return Keyword != null && Value != null;
            }
        }

        public override string ToString()
        {
            string text = Keyword.Text;

            if (Value != null)
                text += $" = {Value.Text}";

            if (Severity != null)
                text += $" : {Severity.Text}";

            return text;
        }
    }
}
