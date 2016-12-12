using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        private string _fileName;
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

            _fileName = _fileName ?? TextBuffer.GetFileName();

            if (!File.Exists(_fileName))
                return null;

            var file = new FileInfo(_fileName);
            var parent = file.Directory.Parent;

            while (parent != null)
            {
                parentFileName = Path.Combine(parent.FullName, Constants.FileName);

                if (File.Exists(parentFileName))
                {
                    var doc = DocumentService.CreateAndLoadTextDocument(parentFileName, _contentType);
                    return new EditorConfigDocument(doc.TextBuffer) { _fileName = parentFileName };
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
