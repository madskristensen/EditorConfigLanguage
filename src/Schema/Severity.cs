using Microsoft.VisualStudio.Imaging.Interop;

namespace EditorConfig
{
    public class Severity : ISchemaItem
    {
        public Severity(string name, string description, ImageMoniker moniker)
        {
            Name = name;
            Description = description;
            Moniker = moniker;
        }

        public string Name { get; }
        public string Description { get; }
        public ImageMoniker Moniker { get; }
        public bool IsSupported => true;
    }
}
