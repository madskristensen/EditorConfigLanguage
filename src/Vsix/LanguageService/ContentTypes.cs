using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace EditorConfig
{
    public class ContentTypes
    {
        public const string EditorConfig = "EditorConfig";
        public const string FileName = ".editorconfig";


        [Export(typeof(ContentTypes))]
        [Name(EditorConfig)]
        [BaseDefinition("code")]
        public ContentTypes IEditorConfigContentType { get; set; }
    }
}
