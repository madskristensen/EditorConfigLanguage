using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Linq;

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

        public List<ParseItem> Children { get; } = new List<ParseItem>();

        public ParseItem Parent { get; set; }

        public ParseItem Next { get; set; }

        public ParseItem Prev { get; set; }

        public bool IsValid
        {
            get { return ItemType == ItemType.Unknown; }
        }

        public void AddError(string errorMessage)
        {
            if (!Errors.Contains(errorMessage))
                Errors.Add(errorMessage);
        }

        public void AddChild(ParseItem child)
        {
            Children.Add(child);
            child.Parent = this;
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
