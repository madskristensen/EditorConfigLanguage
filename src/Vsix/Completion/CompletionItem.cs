using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    class CompletionItem
    {
        private static List<CompletionItem> _dic = new List<CompletionItem>
        {
            {new CompletionItem("root", true, Resources.Text.KeywordRoot, "true")},
            {new CompletionItem("charset", true, Resources.Text.KeywordCharset, "latin1", "utf-8", "utf-8-bom", "utf-16be", "utf-16le", "utf-8-bom")},
            {new CompletionItem("end_of_line", true, Resources.Text.KeywordEndOfLine, "lf", "crlf", "cr") },
            {new CompletionItem("indent_style", true, Resources.Text.KeywordIndentStyle, "tab", "space")},
            {new CompletionItem("indent_size", true, Resources.Text.KeywordIndentSize, "tab") },
            {new CompletionItem("tab_width", true, Resources.Text.KeywordTabWidth) },
            {new CompletionItem("trim_trailing_whitespace", false, Resources.Text.KeywordTrimTrailingWhitespace, "true", "false")},
            {new CompletionItem("insert_final_newline", false, Resources.Text.KeywordInsertFinalNewline, "true", "false")},
            //{new CompletionItem("max_line_length", "Forces hard line wrapping after the amount of characters specified (Not supported by Visual Studio)")},
        };

        private CompletionItem(string name, bool isSupported, string description, params string[] values)
        {
            Name = name;
            Description = description;
            Values = values;
            IsSupported = isSupported;
        }

        public static IEnumerable<CompletionItem> Items
        {
            get { return _dic; }
        }

        public static CompletionItem GetCompletionItem(string name)
        {
            return _dic.SingleOrDefault(c => c.Name == name);
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> Values { get; set; }
        public bool IsSupported { get; set; }
    }
}
