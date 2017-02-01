namespace EditorConfigTest
{
    public class Samples
    {
        public const string OneSectionStandard = @"root = true

# comment
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true";

        public const string MultipleValuesSection = @"[*.cs]
csharp_new_line_before_open_brace = accessors, indexers";
    }
}
