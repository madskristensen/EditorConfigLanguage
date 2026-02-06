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
    public class Keyword(string name, string description, IEnumerable<string> values, IEnumerable<string> defaultValue, bool unsupported, bool hidden, bool multiple, bool severity, string defaultSeverity, string documentationLink, string example) : ITooltip
    {

        /// <summary>The keyword of the property.</summary>
        public string Name { get; } = name;

        /// <summary>The description of the property.</summary>
        public string Description { get; } = description;

        /// <summary>A list of values applicable to the property.</summary>
        public IEnumerable<Value> Values { get; } = [.. values.Select(v => new Value(v))];

        /// <summary>The default value(s) for the property.</summary>
        public IEnumerable<Value> DefaultValue { get; } = [.. defaultValue.Select(v => new Value(v))];

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
                    if (Name.StartsWith("csharp_", StringComparison.OrdinalIgnoreCase))
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
