using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    public class ContentTypes
    {
        public const string EditorConfig = "EditorConfig";

        [Export(typeof(ContentTypes))]
        [Name(EditorConfig)]
        [BaseDefinition("code")]
        public ContentTypes IEditorConfigContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(EditorConfig)]
        [FileExtension(".editorconfig")]
        public FileExtensionToContentTypeDefinition EditorConfigFileExtension { get; set; }
    }
}
