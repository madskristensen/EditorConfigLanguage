using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace EditorConfig
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Constants.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class ErrorList : IWpfTextViewCreationListener
    {
        private EditorConfigValidator _validator;
        private EditorConfigDocument _document;
        private Project _project;
        private string _file;

        [Import]
        private ITextDocumentFactoryService _documentService = null;

        public void TextViewCreated(IWpfTextView view)
        {
            if (_documentService.TryGetTextDocument(view.TextBuffer, out var doc))
            {
                _file = doc.FilePath;
                view.Properties.AddProperty("file", doc.FilePath);
            }

            _document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
            _validator = EditorConfigValidator.FromDocument(_document);
            _project = VsHelpers.DTE.Solution?.FindProjectItem(_file)?.ContainingProject;

            view.Closed += ViewClosed;
            _validator.Validated += Validated;
        }

        private void Validated(object sender, EventArgs e)
        {
            UpdateErrorList();
        }

        private void UpdateErrorList()
        {
            if (_document.IsParsing)
                return;

            ParseItem[] items = _document.ParseItems.Where(p => p.HasErrors).ToArray();

            TableDataSource.Instance.CleanErrors(_file);
            TableDataSource.Instance.AddErrors(items, _project?.Name, _file);
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;
            view.Closed -= ViewClosed;

            if (view.Properties.TryGetProperty("file", out string file))
            {
                TableDataSource.Instance.CleanErrors(file);
            }
        }
    }
}
