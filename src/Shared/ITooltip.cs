using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public interface ITooltip
    {
        string Name { get; }
        string Description { get; }
        ImageMoniker Moniker { get; }
        bool IsSupported { get; }
    }
}