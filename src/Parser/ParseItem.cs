using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class ParseItem
    {
        public ParseItem(EditorConfigDocument document, ItemType type, Span span, string text)
        {
            Document = document;
            ItemType = type;
            Span = span;
            Text = text;
        }

        public EditorConfigDocument Document { get; set; }

        public Span Span { get; set; }

        public ItemType ItemType { get; set; }

        public string Text { get; set; }

        public List<Error> Errors { get; } = new List<Error>();

        public bool HasErrors
        {
            get { return Errors.Any(); }
        }

        public void AddError(int errorCode, string errorMessage, ErrorType errorType)
        {
            if (!Errors.Any(e => e.Name.Equals(errorMessage, System.StringComparison.OrdinalIgnoreCase)))
                Errors.Add(new Error(errorCode, errorMessage, errorType));
        }

        public override string ToString()
        {
            return ItemType + ": " + Text;
        }

        public override int GetHashCode()
        {
            int textHash = string.IsNullOrEmpty(Text) ? 1 : Text.GetHashCode();
            return Span.GetHashCode() ^ textHash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ParseItem other))
                return false;

            return Equals(other);
        }

        public bool Equals(ParseItem other)
        {
            if (other == null)
                return false;

            if (Span != other.Span)
                return false;

            return Text.Equals(other.Text, System.StringComparison.Ordinal);
        }

        public static bool operator ==(ParseItem a, ParseItem b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(ParseItem a, ParseItem b)
        {
            return !(a == b);
        }
    }

    public enum ItemType
    {
        Comment,
        Section,
        Property,
        Value,
        Severity,
        Unknown
    }
}
