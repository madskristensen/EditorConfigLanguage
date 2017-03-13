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

        public const string SeveritySimple = @"[*.cs]
dotnet_style_qualification_for_event = true";

        public const string Suppression = @"# Suppress: EC101 EC102";

        public const string NamingRules = @"[*.cs]
dotnet_naming_rule.foo.severity = warning
dotnet_naming_rule.foo.symbols = fooSymbolsTitle
dotnet_naming_rule.foo.style = fooStyleTitle";
    }
}
