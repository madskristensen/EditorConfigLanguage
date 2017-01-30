using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    /// <summary>The severity determines how the property is enforced by Visual Studio.</summary>
    public class Severity : ITooltip
    {
        public Severity(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>The severity name.</summary>
        public string Name { get; }

        /// <summary>The severity description.</summary>
        public string Description { get; }

        /// <summary>True if Visual Studio supports the severity.</summary>
        public bool IsSupported => true;

        /// <summary>The image moniker shown by Intellisense and the adornment next to the severity.</summary>
        public ImageMoniker Moniker
        {
            get
            {
                switch (Name)
                {
                    case "none":
                        return KnownMonikers.None;
                    case "suggestion":
                        return KnownMonikers.StatusInformation;
                    case "warning":
                        return KnownMonikers.StatusWarning;
                    case "error":
                        return KnownMonikers.StatusError;
                }

                return KnownMonikers.UnknownMember;
            }
        }
    }
}
