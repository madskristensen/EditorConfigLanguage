using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public class Severity : ITooltip
    {
        public Severity(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }
        public bool IsSupported => true;
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
