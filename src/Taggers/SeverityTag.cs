using Microsoft.VisualStudio.Text.Tagging;

namespace EditorConfig
{
    internal class SeverityTag : ITag
    {
        public SeverityTag(ParseItem parseItem)
        {
            ParseItem = parseItem;
        }

        public ParseItem ParseItem { get; }
    }
}
