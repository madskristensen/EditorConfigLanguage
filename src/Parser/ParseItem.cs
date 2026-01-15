using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    /// <summary>A building block of the document.</summary>
    public class ParseItem(EditorConfigDocument document, ItemType type, Span span, string text)
    {

        /// <summary>The document this item belongs to.</summary>
        public EditorConfigDocument Document { get; set; } = document;

        /// <summary>The span of this item in the text buffer.</summary>
        public Span Span { get; set; } = span;

        /// <summary>The type of item.</summary>
        public ItemType ItemType { get; set; } = type;

        /// <summary>The text of this item in the text buffer.</summary>
        public string Text { get; set; } = text;

        /// <summary>A list of validation errors.</summary>
        public List<DisplayError> Errors { get; } = [];

        /// <summary>True if the item contains errors; otherwise false.</summary>
        public bool HasErrors
        {
            get { return Errors.Any(); }
        }

        /// <summary>Adds an error to the Errors list if it doesn't already contain it.</summary>
        public void AddError(DisplayError error)
        {
            if (!Errors.Any(e => e.Name == error.Name))
                Errors.Add(error);
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
            if (obj is not ParseItem other)
                return false;

            return Equals(other);
        }

        public bool Equals(ParseItem other)
        {
            if (other == null)
                return false;

            if (Span != other.Span)
                return false;

            return Text.Is(other.Text);
        }

        public static bool operator ==(ParseItem a, ParseItem b)
        {
            if (a is null)
                return b is null;

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
        Keyword,
        Value,
        Severity,
        Suppression,
        Unknown
    }
}
