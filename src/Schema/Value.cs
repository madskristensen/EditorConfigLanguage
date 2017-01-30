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
            _isUnset = string.Equals(name, "unset", StringComparison.OrdinalIgnoreCase);

            Name = name;
            Description = _isUnset ? Resources.Text.ValueUnset : null;
            IsSupported = !_isUnset;
            Moniker = KnownMonikers.EnumerationItemPublic;
        }

        public string Name { get; }
        public string Description { get; }
        public ImageMoniker Moniker { get; }
        public bool IsSupported { get; }
    }
}
