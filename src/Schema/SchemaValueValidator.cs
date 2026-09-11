using System;
using System.Linq;

namespace EditorConfig
{
    internal static class SchemaValueValidator
    {
        internal const string FreeForm = "free_form";
        internal const string Integer = "integer";
        internal const string LanguageTag = "language_tag";

        internal static bool IsValid(Keyword keyword, string value)
        {
            if (keyword == null || value == null)
                return false;

            if (keyword.SupportsMultipleValues)
            {
                string[] values = value.Split([','], StringSplitOptions.RemoveEmptyEntries);
                return values.Length > 0 && values.All(item => IsSingleValueValid(keyword, item.Trim()));
            }

            return IsSingleValueValid(keyword, value.Trim());
        }

        private static bool IsSingleValueValid(Keyword keyword, string value)
        {
            if (keyword.Values.Any(candidate => candidate.Name.Is(value)) ||
                keyword.ValueAliases.ContainsKey(value))
            {
                return true;
            }

            if (keyword.ValueKind.Is(FreeForm))
                return true;

            if (keyword.ValueKind.Is(LanguageTag))
                return Bcp47LanguageTag.IsValid(value);

            if (keyword.ValueKind.Is(Integer))
            {
                return int.TryParse(value, out int number) &&
                    (!keyword.Minimum.HasValue || number >= keyword.Minimum.Value) &&
                    (!keyword.Maximum.HasValue || number <= keyword.Maximum.Value);
            }

            if (!keyword.SupportsMultipleValues && !keyword.Values.Any())
                return true;

            if (keyword.Values.Any(candidate => IsPlaceholder(candidate.Name)))
                return true;

            return int.TryParse(value, out int positiveInteger) && positiveInteger > 0;
        }

        private static bool IsPlaceholder(string value)
            => value?.Length > 2 && value[0] == '<' && value[value.Length - 1] == '>';
    }
}
