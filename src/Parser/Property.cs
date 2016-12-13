using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    public class Property
    {
        public Property(ParseItem keyword)
        {
            Keyword = keyword;
        }

        public ParseItem Keyword { get; set; }
        public ParseItem Value { get; set; }
        public ParseItem Severity { get; set; }

        public Span Span
        {
            get
            {
                var last = Severity ?? Value ?? Keyword;
                return Span.FromBounds(Keyword.Span.Start, last.Span.End);
            }
        }

        public bool IsValid
        {
            get
            {
                return Keyword != null && Value != null;
            }
        }

        public override string ToString()
        {
            var text = Keyword.Text;

            if (Value != null)
                text += $" = {Value.Text}";

            if (Severity != null)
                text += $" : {Severity.Text}";

            return text;
        }
    }
}
