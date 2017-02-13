using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;

namespace EditorConfig
{
    internal sealed class SuppressError
    {
        private readonly Package _package;
        private Error _selectedError;

        private SuppressError(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var cmdId = new CommandID(PackageGuids.guidEditorConfigPackageCmdSet, PackageIds.SuppressErrorId);
            var menuItem = new OleMenuCommand(Execute, cmdId);
            menuItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        public static SuppressError Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new SuppressError(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Enabled = button.Visible = false;

            var errorList = VsHelpers.DTE.ToolWindows.ErrorList as IErrorList;
            ITableEntryHandle entry = errorList.TableControl.SelectedEntry;

            if (entry == null || !entry.TryGetValue(StandardTableKeyNames.ErrorCode, out string content))
                return;

            if (!ErrorCatalog.TryGetErrorCode(content, out _selectedError))
                return;

            if (entry == null || !entry.TryGetValue(StandardTableKeyNames.DocumentName, out string document))
                return;

            if (document != VsHelpers.DTE.ActiveDocument.FullName)
                return;

            button.Visible = button.Enabled = true;
            button.Text = "Suppress " + _selectedError.Code;
        }

        private void Execute(object sender, EventArgs e)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(1, null, out IVsTextView activeView));
            IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            IWpfTextView wpfTextView = editorAdapter.GetWpfTextView(activeView);
            ITextBuffer buffer = wpfTextView.TextBuffer;

            var document = EditorConfigDocument.FromTextBuffer(buffer);
            var validator = EditorConfigValidator.FromDocument(document);
            validator.SuppressError(_selectedError.Code);
        }
    }
}
