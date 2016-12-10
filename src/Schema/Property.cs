using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.Linq;
using System;

namespace EditorConfig
{
    public class Property : ISchemaItem
    {        
        public Property(string name, bool isSupported, string description, ImageMoniker moniker, params string[] values)
        {
            Name = name;
            Description = description;
            Values = values;
            Moniker = moniker;
            IsSupported = isSupported;

            if (name.StartsWith("dotnet"))
                Tag = "dotnet";
            else if (name.StartsWith("csharp"))
                Tag = "csharp";
            else
                Tag = "standard";
        }
       
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<string> Values { get; set; }
        public bool IsSupported { get; set; }
        public ImageMoniker Moniker { get; set; }
        public string Tag { get; set; }
        public bool SupportsSeverity
        {
            get
            {
                return Name.StartsWith("csharp_", StringComparison.OrdinalIgnoreCase) || Name.StartsWith("dotnet_", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
