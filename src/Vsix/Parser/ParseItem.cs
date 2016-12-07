using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;

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
        public string Description
        {
            get { return Keyword.GetCompletionItem(Text)?.Description; }
        }
        public List<string> Errors { get; } = new List<string>();
        public bool HasErrors
        {
            get { return Errors.Any(); }
        }

        public void AddError(string errorMessage)
        {
            if (!Errors.Contains(errorMessage))
                Errors.Add(errorMessage);
        }
    }

    public enum ItemType
    {
        Comment,
        Section,
        Keyword,
        Value,
        Severity
    }
}
