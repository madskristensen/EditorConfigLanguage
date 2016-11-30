using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    class CompletionItem
    {
        private static List<CompletionItem> _dic = new List<CompletionItem>
        {
            // Standard properties
            {new CompletionItem("root", true, Resources.Text.KeywordRoot, KnownMonikers.ApplicationRoot, "true")},
            {new CompletionItem("charset", true, Resources.Text.KeywordCharset, KnownMonikers.Property, "latin1", "utf-8", "utf-8-bom", "utf-16be", "utf-16le", "utf-8-bom")},
            {new CompletionItem("end_of_line", true, Resources.Text.KeywordEndOfLine, KnownMonikers.Property, "lf", "crlf", "cr") },
            {new CompletionItem("indent_style", true, Resources.Text.KeywordIndentStyle, KnownMonikers.Property, "tab", "space")},
            {new CompletionItem("indent_size", true, Resources.Text.KeywordIndentSize, KnownMonikers.Property, "tab") },
            {new CompletionItem("tab_width", true, Resources.Text.KeywordTabWidth, KnownMonikers.Property) },
            {new CompletionItem("trim_trailing_whitespace", false, Resources.Text.KeywordTrimTrailingWhitespace, KnownMonikers.Property, "true", "false")},
            {new CompletionItem("insert_final_newline", false, Resources.Text.KeywordInsertFinalNewline, KnownMonikers.Property, "true", "false")},
            //{new CompletionItem("max_line_length", "Forces hard line wrapping after the amount of characters specified (Not supported by Visual Studio)")},

            // C# code analysis
            {new CompletionItem("csharp_style_conditional_delegate_call", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_expression_bodied_accessors", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_expression_bodied_constructors", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_expression_bodied_indexers", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_expression_bodied_methods", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_expression_bodied_operators", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_expression_bodied_properties", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_inlined_variable_declaration", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_pattern_matching_over_as_with_null_check", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_pattern_matching_over_is_with_cast_check", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_throw_expression", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_var_elsewhere", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_var_for_built_in_types", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_var_for_locals", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},
            {new CompletionItem("csharp_style_var_when_type_is_apparent", true, "C# code analysis rule", KnownMonikers.CSFileNode, "true", "false")},

            // .NET style analysis
            {new CompletionItem("dotnet_style_coalesce_expression", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_collection_initializer ", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_null_propagation ", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_object_initializer", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_predefined_type_for_locals_parameters_members", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_predefined_type_for_member_access", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_qualification_for_event", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_qualification_for_field", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_qualification_for_method", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
            {new CompletionItem("dotnet_style_qualification_for_property", true, "C# code analysis rule", KnownMonikers.DotNET, "true", "false")},
        };

        private CompletionItem(string name, bool isSupported, string description, ImageMoniker moniker, params string[] values)
        {
            Name = name;
            Description = description;
            Values = values;
            Moniker = moniker;
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
        public ImageMoniker Moniker { get; set; }
    }
}
