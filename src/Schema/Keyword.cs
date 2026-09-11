using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    /// <summary>The keyword is the name-part of a property.</summary>
    /// <remarks>
    /// Creates a keyword from JSON deserialization.
    /// </remarks>
    public class Keyword(
        string name,
        string description,
        IEnumerable<string> values,
        IEnumerable<string> defaultValue,
        bool unsupported,
        bool hidden,
        bool multiple,
        bool severity,
        string defaultSeverity,
        string documentationLink,
        string example,
        string valueKind = null,
        int? minimum = null,
        int? maximum = null,
        bool deprecated = false,
        string replacement = null,
        IEnumerable<string> aliases = null,
        IDictionary<string, string> valueAliases = null,
        string declarationKind = null,
        string referenceKind = null) : ITooltip
    {

        /// <summary>The keyword of the property.</summary>
        public string Name { get; } = name;

        /// <summary>The description of the property.</summary>
        public string Description { get; } = description;

        /// <summary>A list of values applicable to the property.</summary>
        public IEnumerable<Value> Values { get; } = [.. (values ?? []).Select(v => new Value(v))];

        /// <summary>The default value(s) for the property.</summary>
        public IEnumerable<Value> DefaultValue { get; } = [.. (defaultValue ?? []).Select(v => new Value(v))];

        /// <summary>True if the property is supported by Visual Studio.</summary>
        public bool IsSupported { get; } = !unsupported;

        /// <summary>True if the property shows up in Intellisense.</summary>
        public bool IsVisible { get; } = !hidden;

        /// <summary>True if the value can be a comma separated list.</summary>
        public bool SupportsMultipleValues { get; } = multiple;

        public bool RequiresSeverity { get; } = severity;

        public string DefaultSeverity { get; } = defaultSeverity;

        /// <summary>Link to the property's documentation. Null if no documentation.</summary>
        public string DocumentationLink { get; } = documentationLink;

        /// <summary>A code example showing the effect of this property.</summary>
        public string Example { get; } = example;

        /// <summary>Optional validation kind for values not fully represented by <see cref="Values"/>.</summary>
        public string ValueKind { get; } = valueKind;

        /// <summary>Optional inclusive lower bound for integer values.</summary>
        public int? Minimum { get; } = minimum;

        /// <summary>Optional inclusive upper bound for integer values.</summary>
        public int? Maximum { get; } = maximum;

        /// <summary>True when this property remains accepted but should no longer be used.</summary>
        public bool IsDeprecated { get; } = deprecated;

        /// <summary>The preferred property name when this property is deprecated.</summary>
        public string Replacement { get; } = replacement;

        /// <summary>Alternative property names accepted as aliases.</summary>
        public IReadOnlyList<string> Aliases { get; } = [.. aliases ?? []];

        /// <summary>Alternative value spellings mapped to their canonical values.</summary>
        public IReadOnlyDictionary<string, string> ValueAliases { get; } =
            new Dictionary<string, string>(valueAliases ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);

        /// <summary>The declaration group introduced by a compound property key.</summary>
        public string DeclarationKind { get; } = declarationKind;

        /// <summary>The declaration group referenced by this property's value.</summary>
        public string ReferenceKind { get; } = referenceKind;

        internal bool MatchesName(string candidate)
        {
            if (Name.Equals(candidate, StringComparison.OrdinalIgnoreCase) ||
                Aliases.Any(alias => alias.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return TryGetPlaceholderValue(candidate, out _);
        }

        internal bool TryGetPlaceholderValue(string candidate, out string value)
        {
            value = null;
            if (candidate == null)
                return false;

            int templatePosition = 0;
            int candidatePosition = 0;

            while (templatePosition < Name.Length)
            {
                int placeholderStart = Name.IndexOf('<', templatePosition);
                if (placeholderStart < 0)
                {
                    return string.Equals(
                        Name.Substring(templatePosition),
                        candidate.Substring(candidatePosition),
                        StringComparison.OrdinalIgnoreCase);
                }

                string literal = Name.Substring(templatePosition, placeholderStart - templatePosition);
                if (candidatePosition + literal.Length > candidate.Length ||
                    !candidate.Substring(candidatePosition, literal.Length).Equals(literal, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                candidatePosition += literal.Length;
                int placeholderEnd = Name.IndexOf('>', placeholderStart + 1);
                if (placeholderEnd < 0)
                    return false;

                int nextPlaceholder = Name.IndexOf('<', placeholderEnd + 1);
                int nextLiteralEnd = nextPlaceholder < 0 ? Name.Length : nextPlaceholder;
                string nextLiteral = Name.Substring(placeholderEnd + 1, nextLiteralEnd - placeholderEnd - 1);
                int valueEnd = nextLiteral.Length == 0
                    ? candidate.Length
                    : candidate.IndexOf(nextLiteral, candidatePosition, StringComparison.OrdinalIgnoreCase);

                if (valueEnd <= candidatePosition)
                    return false;

                value ??= candidate.Substring(candidatePosition, valueEnd - candidatePosition);
                candidatePosition = valueEnd;
                templatePosition = placeholderEnd + 1;
            }

            return candidatePosition == candidate.Length;
        }

        /// <summary>
        /// The name of the custom extension that provided this keyword, or null for built-in keywords.
        /// </summary>
        public string CustomExtensionName { get; internal set; }

        /// <summary>
        /// Custom moniker for this keyword when provided by an extension, or default if not set.
        /// </summary>
        public ImageMoniker CustomMoniker { get; internal set; }

        /// <summary>The category is used in the Intellisense filters.</summary>
        public Category Category
        {
            get
            {
                // Custom extension keywords have their own category
                if (!string.IsNullOrEmpty(CustomExtensionName))
                {
                    return Category.Custom;
                }

                if (!string.IsNullOrWhiteSpace(Name))
                {
                    if (Name.Equals("spelling_language", StringComparison.OrdinalIgnoreCase))
                        return Category.Standard;
                    else if (Name.StartsWith("csharp_", StringComparison.OrdinalIgnoreCase))
                        return Category.CSharp;
                    else if (Name.StartsWith("dotnet_", StringComparison.OrdinalIgnoreCase))
                        return Category.DotNet;
                    else if (Name.StartsWith("visual_basic_", StringComparison.OrdinalIgnoreCase))
                        return Category.VisualBasic;
                    else if (Name.StartsWith("cpp_", StringComparison.OrdinalIgnoreCase))
                        return Category.CPP;
                    else if (Name.StartsWith("spelling_", StringComparison.OrdinalIgnoreCase))
                        return Category.VisualStudio;
                    else
                        return Category.Standard;
                }

                return Category.None;
            }
        }

        /// <summary>The image moniker that represents the property.</summary>
        public ImageMoniker Moniker
        {
            get
            {
                // Use custom moniker if set
                if (CustomMoniker.Guid != default)
                {
                    return CustomMoniker;
                }

                return Category switch
                {
                    Category.CSharp => KnownMonikers.CSFileNode,
                    Category.DotNet => KnownMonikers.DotNET,
                    Category.VisualBasic => KnownMonikers.VBFileNode,
                    Category.CPP => KnownMonikers.CPPFileNode,
                    Category.VisualStudio => KnownMonikers.VisualStudio,
                    _ => KnownMonikers.Property,
                };
            }
        }
    }
}
