using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class Keyword : ISchemaItem
    {
        public Keyword(string name, string description, Category category, params string[] values)
        {
            Name = name;
            Description = description;
            Values = values.Select(v => new Value(v));
            Category = category;
            IsSupported = true;
        }

        public string Name { get; }
        public string Description { get; }
        public IEnumerable<Value> Values { get; }
        public bool IsSupported { get; set; }
        public Category Category { get; }
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

        public bool SupportsSeverity
        {
            get
            {
                return Category == Category.CSharp || Category == Category.DotNet;
            }
        }
    }

    public enum Category
    {
        None,
        Standard,
        CSharp,
        DotNet,
    }
}
