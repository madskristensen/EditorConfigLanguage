using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    /// <summary>The keyword is the name-part of a property.</summary>
    public class Keyword : ITooltip
    {
        public Keyword(string name, string description, IEnumerable<string> values, IEnumerable<string> defaultValue, bool unsupported, bool hidden, bool multiple, bool severity, string defaultSeverity)
        {
            Name = name;
            Description = description;
            Values = values.Select(v => new Value(v));
            DefaultValue = defaultValue.Select(v => new Value(v));
            IsSupported = !unsupported;
            IsVisible = !hidden;
            SupportsMultipleValues = multiple;
            RequiresSeverity = severity;
            DefaultSeverity = defaultSeverity;
        }

        /// <summary>The keyword of the property.</summary>
        public string Name { get; }

        /// <summary>The description of the property.</summary>
        public string Description { get; }

        /// <summary>A list of values applicable to the property.</summary>
        public IEnumerable<Value> Values { get; }

        public IEnumerable<Value> DefaultValue { get; }

        /// <summary>True if the property is supported by Visual Studio.</summary>
        public bool IsSupported { get; }

        /// <summary>True if the property shows up in Intellisense.</summary>
        public bool IsVisible { get; }

        /// <summary>True if the value can be a comman separated list.</summary>
        public bool SupportsMultipleValues { get; }

        public bool RequiresSeverity { get; }
        public string DefaultSeverity { get; }

        /// <summary>The category is used in the Intellisense filters.</summary>
        public Category Category
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    if (Name.StartsWith("csharp_", StringComparison.OrdinalIgnoreCase))
                        return Category.CSharp;
                    else if (Name.StartsWith("dotnet_", StringComparison.OrdinalIgnoreCase))
                        return Category.DotNet;
                    else if (Name.StartsWith("visual_basic_", StringComparison.OrdinalIgnoreCase))
                        return Category.VisualBasic;
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
                switch (Category)
                {
                    case Category.CSharp:
                        return KnownMonikers.CSFileNode;
                    case Category.DotNet:
                        return KnownMonikers.DotNET;
                    case Category.VisualBasic:
                        return KnownMonikers.VBFileNode;
                }

                return KnownMonikers.Property;
            }
        }
    }
}
