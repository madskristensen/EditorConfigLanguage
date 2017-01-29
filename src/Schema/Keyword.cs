using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class Keyword : ITooltip
    {
        public Keyword(string name, string description, IEnumerable<string> values, bool unsupported, bool hidden)
        {
            Name = name;
            Description = description;
            Values = values.Select(v => new Value(v));
            IsSupported = !unsupported;
            IsVisible = !hidden;
        }

        public string Name { get; }
        public string Description { get; }
        public IEnumerable<Value> Values { get; }
        public bool IsSupported { get; }
        public bool IsVisible { get; }

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
                    else
                        return Category.Standard;
                }

                return Category.None;
            }
        }

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
                }

                return KnownMonikers.Property;
            }
        }

        public bool RequiresSeverity
        {
            get
            {
                return Name.StartsWith("csharp_style") || Name.StartsWith("dotnet_style");
            }
        }
    }
}
