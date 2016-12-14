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

        public void AddError(string errorMessage)
        {
            AddError(errorMessage, ErrorType.Error);
        }

        public void AddError(string errorMessage, ErrorType errorType)
        {
            if (!Errors.Any(e => e.Name.Equals(errorMessage, System.StringComparison.OrdinalIgnoreCase)))
                Errors.Add(new Error(errorMessage, errorType));
        }

        public override string ToString()
        {
            return ItemType + ": " + Text;
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
