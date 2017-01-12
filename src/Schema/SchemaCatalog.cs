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
            yield return new Keyword("csharp_style_conditional_delegate_call", "Prefer to use conditional coalescing operation (?.) when invoking a lambda instead of performing a null check (e.g., 'func?.Invoke(args);').", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_accessors", "Prefer expression-bodied members for accessors (e.g., 'public int Age { get => _age; set => _age = value; }').", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_constructors", "Prefer expression-bodied members for constructors (e.g., 'public Customer(int age) => Age = age;').", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_indexers", "Prefer expression-bodied members for indexers (e.g., 'public T this[int i] => _value[i];').", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_methods", "Prefer expression-bodied members for methods (e.g., 'public int GetAge() => this.Age;').", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_operators", "Prefer expression-bodied members for operators.", category, "true", "false");
            yield return new Keyword("csharp_style_expression_bodied_properties", "Prefer expression-bodied members for properties (e.g., 'public int Age => _age;').", category, "true", "false");
            yield return new Keyword("csharp_style_inlined_variable_declaration", "Prefer 'out' variables to be declared inline when possible (e.g., 'if (int.TryParse(value out int i) {...}').", category, "true", "false");
            yield return new Keyword("csharp_style_pattern_matching_over_as_with_null_check", "Prefer pattern matching instead of 'as' expressions with null-checks to determine if something is of a particular type (e.g., 'if (o is string s) {...}').", category, "true", "false");
            yield return new Keyword("csharp_style_pattern_matching_over_is_with_cast_check", "Prefer pattern matching instead of 'is' expressions with type casts (e.g., 'if (o is int i) {...}').", category, "true", "false");
            yield return new Keyword("csharp_style_throw_expression", "Prefer to use 'throw' expressions instead of 'throw' statements.", category, "true", "false");
            yield return new Keyword("csharp_style_var_elsewhere", "Prefer 'var' in all cases unless overridden by another code style rule.", category, "true", "false");
            yield return new Keyword("csharp_style_var_for_built_in_types", "Prefer 'var' is used for built-in system types such as 'int'.", category, "true", "false");
            yield return new Keyword("csharp_style_var_when_type_is_apparent", "Prefer 'var' when the type is already mentioned on the right-hand side of a declaration expression.", category, "true", "false");

            category = Category.DotNet;
            yield return new Keyword("dotnet_sort_system_directives_first", "Prefer to place 'System' directives first when sorting usings.", category, "true", "false");
            yield return new Keyword("dotnet_style_coalesce_expression", "Prefer null coalescing expression to ternary operator checking (e.g. 'var v = x ?? y;').", category, "true", "false");
            yield return new Keyword("dotnet_style_collection_initializer", "Prefer collections to be initialized using collection initializers when possible (e.g., 'var list = new List<int>{ 1, 2, 3 };').", category, "true", "false");
            yield return new Keyword("dotnet_style_explicit_tuple_names", "Prefer tuple names to ItemX properties (e.g., '(string name, int age) customer = GetCustomer(); var name = customer.name;').", category, "true", "false");
            yield return new Keyword("dotnet_style_null_propagation", "Prefer to use null-conditional operator where possible (e.g., 'var v = o?.ToString();').", category, "true", "false");
            yield return new Keyword("dotnet_style_object_initializer", "Prefer objects to be initialized using object initializers when possible (e.g., 'var c = new Customer(){ Age = 21 };').", category, "true", "false");
            yield return new Keyword("dotnet_style_predefined_type_for_locals_parameters_members", "For locals, parameters and type members, prefer types that have a language keyword to represent them (int, double, string, etc.) to use that keyword instead of the type name (Int32, Int64, etc.).", category, "true", "false");
            yield return new Keyword("dotnet_style_predefined_type_for_member_access", "Prefer the keyword whenever a member-access expression is used on a type with a keyword representation (int, double, string, etc.).", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_event", "Prefer all non-static events referenced from within non-static methods to be prefaced with 'this.' in C# and 'Me.' in Visual Basic.", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_field", "Prefer all non-static fields used in non-static methods to be prefaced with 'this.' in C# or 'Me.' in Visual Basic.", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_method", "Prefer all non-static methods called from within non-static methods to be prefaced with 'this.' in C# and 'Me.' in Visual Basic.", category, "true", "false");
            yield return new Keyword("dotnet_style_qualification_for_property", "Prefer the all non-static properties used in non-static methods to be prefaced with 'this.' in C# or 'Me.' in Visual Basic.", category, "true", "false");
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
