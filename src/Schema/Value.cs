using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using System;

namespace EditorConfig
{
    /// <summary>The value of a property.</summary>
    public class Value : ITooltip
    {
        private bool _isUnset;

        public Value(string name)
        {
            _isUnset = name.Is("unset");

            Name = name;
            Description = GetDescription();
            IsSupported = !_isUnset;
            Moniker = KnownMonikers.EnumerationItemPublic;
        }

        /// <summary>The value text.</summary>
        public string Name { get; }

        /// <summary>The value description.</summary>
        public string Description { get; }

        /// <summary>The image moniker shown in Intellisense and QuickInfo.</summary>
        public ImageMoniker Moniker { get; }

        /// <summary>True if the value is supported by Visual Studio.</summary>
        public bool IsSupported { get; }

        private string GetDescription()
        {
            if (_isUnset)
                return Resources.Text.ValueUnset;

            return null;
        }
    }
}
