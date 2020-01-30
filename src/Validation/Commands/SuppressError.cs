using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;

namespace EditorConfig
{
    internal sealed class SuppressError
    {
        private readonly Package _package;
        private Error _selectedError;
        private string _filePath;

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

            if (entry == null || !entry.TryGetValue(StandardTableKeyNames.DocumentName, out _filePath))
                return;

            button.Visible = button.Enabled = true;
            button.Text = "Suppress " + _selectedError.Code;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (TextViewUtil.TryGetWpfTextView(_filePath, out IWpfTextView view))
                {
                    var document = EditorConfigDocument.FromTextBuffer(view.TextBuffer);
                    var validator = EditorConfigValidator.FromDocument(document);
                    validator.SuppressError(_selectedError.Code);
                }
            }
            catch (Exception ex)
            {
                Telemetry.TrackException("SuppressError", ex);
            }
        }
    }
}
