using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;

namespace EditorConfig
{
    /// <summary>
    /// Builds QuickInfo tooltips using ClassifiedTextRun for automatic VS theme support
    /// and syntax highlighting capabilities.
    /// </summary>
    internal static class QuickInfoBuilder
    {
        private const int MaxDescriptionLength = 200;

        /// <summary>
        /// Builds a tooltip for any ITooltip implementation (Keyword, Value, Severity, Error, etc.)
        /// </summary>
        public static ContainerElement BuildTooltip(ITooltip item)
        {
            var elements = new List<object>
            {
                // Header row: Icon + Name (bold)
                BuildHeader(item)
            };

            // Description
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                elements.Add(BuildDescription(item));
            }

            // Unsupported warning
            if (!item.IsSupported)
            {
                elements.Add(BuildUnsupportedWarning());
            }

            // Code example for Keywords
            if (item is Keyword keyword)
            {
                if (!string.IsNullOrEmpty(keyword.Example))
                {
                    elements.Add(ClassifiedTextElement.CreatePlainText(""));
                    elements.Add(BuildCodeExample(keyword));
                }

                // Documentation link (with blank line separator)
                if (!string.IsNullOrEmpty(keyword.DocumentationLink))
                {
                    elements.Add(ClassifiedTextElement.CreatePlainText(""));
                    elements.Add(BuildDocumentationLink(keyword.DocumentationLink));
                }
            }

            var content = new ContainerElement(ContainerElementStyle.Stacked, elements);

            // Wrap in padded container
            return new ContainerElement(
                ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding,
                content);
        }

        private static ClassifiedTextElement BuildHeader(ITooltip item)
        {
            return new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.Literal,
                    PrettifyName(item.Name),
                    ClassifiedTextRunStyle.Bold));
        }

        private static ClassifiedTextElement BuildDescription(ITooltip item)
        {
            string description = item.Description;

            if (description.Length > MaxDescriptionLength)
            {
                description = description.Substring(0, MaxDescriptionLength).TrimEnd() + "...";
            }

            return new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.NaturalLanguage,
                    description));
        }

        private static ContainerElement BuildUnsupportedWarning()
        {
            var warningIcon = new ImageElement(KnownMonikers.StatusWarning.ToImageId());

            var warningText = new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.NaturalLanguage,
                    Resources.Text.NotSupportedByVS,
                    ClassifiedTextRunStyle.Italic));

            return new ContainerElement(
                ContainerElementStyle.Wrapped,
                warningIcon,
                warningText);
        }

        private static ClassifiedTextElement BuildDocumentationLink(string url)
        {
            return new ClassifiedTextElement(
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "Documentation", () => OpenUrl(url)));
        }

        private static ContainerElement BuildCodeExample(Keyword keyword)
        {
            var headerElement = new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.NaturalLanguage,
                    "Example:",
                    ClassifiedTextRunStyle.Bold));

            ClassifiedTextElement codeElement = BuildSyntaxHighlightedCode(keyword.Example, keyword.Category);

            return new ContainerElement(
                ContainerElementStyle.Stacked,
                headerElement,
                codeElement);
        }

        private static ClassifiedTextElement BuildSyntaxHighlightedCode(string code, Category category)
        {
            var runs = new List<ClassifiedTextRun>();

            // Simple tokenization for C#/VB code examples
            string[] tokens = TokenizeCode(code);

            foreach (string token in tokens)
            {
                string classificationType = GetClassificationForToken(token, category);
                runs.Add(new ClassifiedTextRun(classificationType, token));
            }

            return new ClassifiedTextElement(runs);
        }

        private static string[] TokenizeCode(string code)
        {
            var tokens = new List<string>();
            int i = 0;

            while (i < code.Length)
            {
                // Skip and capture whitespace
                if (char.IsWhiteSpace(code[i]))
                {
                    int start = i;
                    while (i < code.Length && char.IsWhiteSpace(code[i]))
                        i++;
                    tokens.Add(code.Substring(start, i - start));
                    continue;
                }

                // String literals
                if (code[i] == '"')
                {
                    int start = i++;
                    while (i < code.Length && code[i] != '"')
                        i++;
                    if (i < code.Length) i++; // Include closing quote
                    tokens.Add(code.Substring(start, i - start));
                    continue;
                }

                // Identifiers and keywords
                if (char.IsLetter(code[i]) || code[i] == '_' || code[i] == '@')
                {
                    int start = i;
                    while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
                        i++;
                    tokens.Add(code.Substring(start, i - start));
                    continue;
                }

                // Numbers
                if (char.IsDigit(code[i]))
                {
                    int start = i;
                    while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.'))
                        i++;
                    tokens.Add(code.Substring(start, i - start));
                    continue;
                }

                // Operators and punctuation (single character)
                tokens.Add(code[i].ToString());
                i++;
            }

            return [.. tokens];
        }

        private static string GetClassificationForToken(string token, Category category)
        {
            // C# keywords
            string[] csharpKeywords = [ "var", "int", "string", "bool", "double", "float", "decimal",
                "char", "byte", "long", "short", "object", "void", "null", "true", "false",
                "new", "public", "private", "protected", "internal", "static", "readonly",
                "const", "class", "struct", "interface", "enum", "namespace", "using",
                "if", "else", "for", "foreach", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "throw", "try", "catch", "finally",
                "async", "await", "get", "set", "this", "base", "is", "as", "in", "out",
                "ref", "params", "virtual", "override", "abstract", "sealed", "partial",
                "where", "select", "from", "orderby", "join", "let", "group", "into" ];

            // VB keywords
            string[] vbKeywords = [ "Dim", "As", "Integer", "String", "Boolean", "Double", "Single",
                "Decimal", "Char", "Byte", "Long", "Short", "Object", "Nothing", "True", "False",
                "New", "Public", "Private", "Protected", "Friend", "Shared", "ReadOnly",
                "Const", "Class", "Structure", "Interface", "Enum", "Namespace", "Imports",
                "If", "Then", "Else", "ElseIf", "End", "For", "Each", "Next", "While", "Do",
                "Loop", "Select", "Case", "Exit", "Continue", "Return", "Throw", "Try", "Catch",
                "Finally", "Async", "Await", "Get", "Set", "Me", "MyBase", "Is", "IsNot",
                "ByRef", "ByVal", "ParamArray", "Overridable", "Overrides", "MustOverride",
                "NotOverridable", "MustInherit", "NotInheritable", "Partial", "Sub", "Function" ];

            string trimmedToken = token.Trim();

            if (string.IsNullOrWhiteSpace(token))
                return PredefinedClassificationTypeNames.WhiteSpace;

            // String literals
            if (trimmedToken.StartsWith("\""))
                return PredefinedClassificationTypeNames.String;

            // Numbers
            if (char.IsDigit(trimmedToken[0]))
                return PredefinedClassificationTypeNames.Number;

            // Keywords based on category
            if (category == Category.CSharp || category == Category.DotNet || category == Category.Standard)
            {
                if (Array.Exists(csharpKeywords, k => k.Equals(trimmedToken, StringComparison.Ordinal)))
                    return PredefinedClassificationTypeNames.Keyword;
            }

            if (category == Category.VisualBasic)
            {
                if (Array.Exists(vbKeywords, k => k.Equals(trimmedToken, StringComparison.OrdinalIgnoreCase)))
                    return PredefinedClassificationTypeNames.Keyword;
            }

            // Type names (PascalCase identifiers that aren't keywords)
            if (char.IsUpper(trimmedToken[0]) && trimmedToken.Length > 1)
                return PredefinedClassificationTypeNames.Type;

            // Operators and punctuation
            if (trimmedToken.Length == 1 && !char.IsLetterOrDigit(trimmedToken[0]))
                return PredefinedClassificationTypeNames.Operator;

            // Default to identifier
            return PredefinedClassificationTypeNames.Identifier;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception)
            {
                // Ignore failures to open URL
            }
        }

        private static string PrettifyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            string text = name
                .Replace("_", " ")
                .Replace("dotnet", ".NET")
                .Replace("csharp", "C#");

            // Capitalize first letter
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
