using Microsoft.VisualStudio.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class SchemaCatalog
    {
        public const string Root = "root";

        public static IEnumerable<Keyword> Properties { get; } = GetProperties();
        public static IEnumerable<Severity> Severities { get; } = GetSeverities();

        private static IEnumerable<Keyword> GetProperties()
        {
            Category category = Category.Standard;
            yield return new Keyword(Root, Schema.Text.KeywordRoot, category, "true", "false");
            yield return new Keyword("charset", Schema.Text.KeywordCharset, category, "latin1", "utf-8", "utf-8-bom", "utf-16be", "utf-16le");
            yield return new Keyword("end_of_line", Schema.Text.KeywordEndOfLine, category, "lf", "crlf", "cr");
            yield return new Keyword("indent_style", Schema.Text.KeywordIndentStyle, category, "tab", "space");
            yield return new Keyword("indent_size", Schema.Text.KeywordIndentSize, category, "2", "tab");
            yield return new Keyword("tab_width", Schema.Text.KeywordTabWidth, category);
            yield return new Keyword("trim_trailing_whitespace", Schema.Text.KeywordTrimTrailingWhitespace, category, "true", "false") { IsSupported = false };
            yield return new Keyword("insert_final_newline", Schema.Text.KeywordInsertFinalNewline, category, "true", "false") { IsSupported = false };
            yield return new Keyword("max_line_length", Schema.Text.KeywordMaxLineLength, category, "80", "off") { IsSupported = false };

            category = Category.CSharp;
            yield return new Keyword("csharp_style_conditional_delegate_call", "Prefer conditional delegate calls.", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_accessors", "Prefer expression-bodied members.", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_constructors", "Prefer expression-bodied members", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_indexers", "Prefer expression-bodied members", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_methods", "Prefer expression-bodied members.", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_operators", "Prefer expression-bodied members.", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_properties", "Prefer expression-bodied members", category, "true", "false");
            yield return new Keyword("csharp_style_inlined_variable_declaration", "Prefer to inline variable declaration.", category, "true", "false");
            yield return new Keyword("csharp_style_pattern_matching_over_as_with_null_check", "Prefer pattern matching.", category, "true", "false");
            yield return new Keyword("csharp_style_pattern_matching_over_is_with_cast_check", "Prefer pattern matching.", category, "true", "false");
            yield return new Keyword("csharp_style_throw_expression", "Prefer throw expressions", category, "true", "false");
            yield return new Keyword("csharp_style_var_elsewhere", "Prefer var elsewhere.", category, "true", "false");
            yield return new Keyword("csharp_style_var_for_built_in_types", "Prefer var for built-in types.", category, "true", "false");
            yield return new Keyword("csharp_style_var_for_locals", "Prefer var for locals.", category, "true", "false");
            yield return new Keyword("csharp_style_var_when_type_is_apparent", "Prefer var when type is apparent.", category, "true", "false");

            category = Category.DotNet;
            yield return new Keyword("dotnet_style_coalesce_expression", "Prefer null coalescing operator (??).", category, "true", "false");
            yield return new Keyword("dotnet_style_collection_initializer", "Prefer collection intiailizers.", category, "true", "false");
            yield return new Keyword("dotnet_style_explicit_tuple_names", "Prefer explicit tuple names.", category, "true", "false");
            yield return new Keyword("dotnet_style_null_propagation", "Prefer null propagation (?.).", category, "true", "false");
            yield return new Keyword("dotnet_style_object_initializer", "Prefer object initializers.", category, "true", "false");
            yield return new Keyword("dotnet_style_predefined_type_for_locals_parameters_members", "Prefer language types (e.g. int, string, float).", category, "true", "false");
            yield return new Keyword("dotnet_style_predefined_type_for_member_access", "Prefer language types (e.g. int, string, float).", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_event", "Prefer this for events.", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_field", "Prefer this for fields.", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_method", "Prefer this for methods.", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_property", "Prefer this for properties.", category, "true", "false");
        }

        private static IEnumerable<Severity> GetSeverities()
        {
            yield return new Severity("none", Schema.Text.SeverityNone, KnownMonikers.None);
            yield return new Severity("suggestion", Schema.Text.SeveritySuggestion, KnownMonikers.StatusInformation);
            yield return new Severity("warning", Schema.Text.SeverityWarning, KnownMonikers.StatusWarning);
            yield return new Severity("error", Schema.Text.SeverityError, KnownMonikers.StatusError);
        }

        public static bool TryGetProperty(string name, out Keyword property)
        {
            property = Properties.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return property != null;
        }

        public static bool TryGetSeverity(string name, out Severity severity)
        {
            severity = Severities.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return severity != null;
        }
    }
}
