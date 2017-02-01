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
        private IContentType _contentType;
        private EditorConfigDocument _parent;

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        /// <summary>The absolute file path to the .editorconfig document.</summary>
        public string FileName { get; private set; }

        /// <summary>Returns a parent document if one exist.</summary>
        public EditorConfigDocument Parent
        {
            get
            {
                if (Root != null && Root.IsValid && Root.Value.Text.Is("true") && Root.Severity == null)
                    return null;

                if (_parent == null)
                    _parent = InheritsFrom();

                return _parent;
            }
        }

        private void InitializeInheritance()
        {
            VsHelpers.SatisfyImportsOnce(this);

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));

            if (componentModel == null)
                return;

            IContentTypeRegistryService contentTypeRegistry = componentModel.DefaultExportProvider.GetExportedValue<IContentTypeRegistryService>();
            _contentType = contentTypeRegistry.GetContentType(Constants.LanguageName);

            if (DocumentService.TryGetTextDocument(TextBuffer, out ITextDocument doc))
            {
                FileName = doc.FilePath;
            }
        }

        private EditorConfigDocument InheritsFrom()
        {
            var file = new FileInfo(FileName);
            DirectoryInfo parent = file.Directory.Parent;

            while (parent != null)
            {
                string parentFileName = Path.Combine(parent.FullName, Constants.FileName);

                if (File.Exists(parentFileName))
                {
                    ITextDocument doc = DocumentService.CreateAndLoadTextDocument(parentFileName, _contentType);
                    return new EditorConfigDocument(doc.TextBuffer) { FileName = parentFileName };
                }

                parent = parent.Parent;
            }

            return null;
        }

        private void DisposeInheritance()
        {
            if (_parent != null)
            {
                _parent.Dispose();
            }
        }
    }
}
