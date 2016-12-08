using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.IO;

namespace EditorConfig
{
    partial class EditorConfigDocument
    {
        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        private string _fileName;

        public EditorConfigDocument InheritsFrom(out string parentFileName)
        {
            parentFileName = null;

            if (IsRoot)
                return null;

            _fileName = _fileName ?? _buffer.GetFileName();

            if (!File.Exists(_fileName))
                return null;

            var file = new FileInfo(_fileName);
            var parent = file.Directory.Parent;

            while (parent != null)
            {
                parentFileName = Path.Combine(parent.FullName, Constants.FileName);

                if (File.Exists(parentFileName))
                {
                    var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                    var contentTypeRegistry = componentModel.DefaultExportProvider.GetExportedValue<IContentTypeRegistryService>();

                    var doc = DocumentService.CreateAndLoadTextDocument(parentFileName, contentTypeRegistry.GetContentType(Constants.LanguageName));
                    return new EditorConfigDocument(doc.TextBuffer) { _fileName = parentFileName };
                }

                parent = parent.Parent;
            }

            return null;
        }
    }
}
