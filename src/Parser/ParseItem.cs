using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace EditorConfig
{
    public class ParseItem
    {
        public ParseItem(ItemType type, Span span, string text)
        {
            ItemType = type;
            Span = span;
            Text = text;
        }

        public Span Span { get; set; }

        public ItemType ItemType { get; set; }

        public string Text { get; set; }

        public List<string> Errors { get; } = new List<string>();

        public void AddError(string errorMessage)
        {
            if (!Errors.Contains(errorMessage))
                Errors.Add(errorMessage);
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
