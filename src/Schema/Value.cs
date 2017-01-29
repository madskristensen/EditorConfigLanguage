using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace EditorConfig
{
    public class Value : ITooltip
    {
        private bool _isUnset;

        public Value(string name)
        {
            Name = name;
            _isUnset = string.Equals(name, "unset", StringComparison.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public string Description
        {
            get
            {
                if (_isUnset)
                    return "For any standard property, a value of \"unset\" is to remove the effect of that property, even if it has been set before.";

                return null;
            }
        }

        public ImageMoniker Moniker => KnownMonikers.EnumerationItemPublic;

        public bool IsSupported
        {
            get { return !_isUnset; }
        }
    }
}
