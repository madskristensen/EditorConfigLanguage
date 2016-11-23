using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    class CompletionItem
    {
        private static List<CompletionItem> _dic = new List<CompletionItem>
        {
            {new CompletionItem("root", true, "Special property that should be specified at the top of the file outside of any sections. Set to “true” to stop .editorconfig files search on current file.", "true")},
            {new CompletionItem("charset", true, "File character encoding, NOTE: if visual studio thinks your file is ascii it will always save it as us-ascii", "latin1", "utf-8", "utf-8-bom", "utf-16be", "utf-16le", "utf-8-bom")},
            {new CompletionItem("end_of_line", true, "Line ending file format (Unix, DOS, Mac)", "lf", "crlf", "cr") },
            {new CompletionItem("indent_style", true, "Indentation Style", "tab", "space")},
            {new CompletionItem("indent_size", true, "A whole number defining the number of columns used for each indentation level and the width of soft tabs (when supported). When set to tab, the value of tab_width (if specified) will be used", "tab") },
            {new CompletionItem("tab_width", true, "A whole number defining the number of columns used to represent a tab character. This defaults to the value of indent_size and doesn't usually need to be specified.") },
            {new CompletionItem("trim_trailing_whitespace", false, "Denotes whether whitespace is allowed at the end of lines", "true", "false")},
            {new CompletionItem("insert_final_newline", false, "Denotes whether file should end with a newline", "true", "false")},
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
