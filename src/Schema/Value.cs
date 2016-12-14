using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public class Value : ITooltip
    {
        public Value(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description { get; }
        public ImageMoniker Moniker => KnownMonikers.EnumerationItemPublic;
        public bool IsSupported => true;
    }
}
