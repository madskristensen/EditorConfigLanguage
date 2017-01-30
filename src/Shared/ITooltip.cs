using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public interface ITooltip
    {
        /// <summary>The name to display in bold letters in the tooltip.</summary>
        string Name { get; }

        /// <summary>The description of the tooltip.</summary>
        string Description { get; }

        /// <summary>The image moniker to represent the tooltip.</summary>
        ImageMoniker Moniker { get; }

        /// <summary>True if its supported by Visual Studio.</summary>
        bool IsSupported { get; }
    }
}