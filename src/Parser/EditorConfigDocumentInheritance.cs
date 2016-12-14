using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace EditorConfig
{
   public partial class EditorConfigDocument
    {
        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public string FileName { get; private set; }
        private IContentType _contentType;

        private void InitializeInheritance()
        {
            VsHelpers.SatisfyImportsOnce(this);

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var contentTypeRegistry = componentModel.DefaultExportProvider.GetExportedValue<IContentTypeRegistryService>();
            _contentType = contentTypeRegistry.GetContentType(Constants.LanguageName);
        }

        public EditorConfigDocument InheritsFrom(out string parentFileName)
        {
            parentFileName = null;

            if (Root != null && Root.IsValid && Root.Value.Text.Equals("true", StringComparison.OrdinalIgnoreCase) && Root.Severity == null)
                return null;

            FileName = FileName ?? TextBuffer.GetFileName();

            if (!File.Exists(FileName))
                return null;

            var file = new FileInfo(FileName);
            var parent = file.Directory.Parent;

            while (parent != null)
            {
                parentFileName = Path.Combine(parent.FullName, Constants.FileName);

                if (File.Exists(parentFileName))
                {
                    var doc = DocumentService.CreateAndLoadTextDocument(parentFileName, _contentType);
                    return new EditorConfigDocument(doc.TextBuffer) { FileName = parentFileName };
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
