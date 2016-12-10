using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public interface ISchemaItem
    {
        string Description { get; set; }
        ImageMoniker Moniker { get; set; }
        string Name { get; set; }
    }
}