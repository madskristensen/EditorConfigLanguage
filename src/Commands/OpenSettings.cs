using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace EditorConfig
{
    internal sealed class OpenSettings
    {
        private readonly Package _package;

        private OpenSettings(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var cmdId = new CommandID(PackageGuids.guidEditorConfigPackageCmdSet, PackageIds.OpenSettingsId);
            var menuItem = new OleMenuCommand(Execute, cmdId);
            menuItem.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Enabled = button.Visible = VsHelpers.DTE.ActiveDocument?.Language == Constants.LanguageName;
        }

        public static OpenSettings Instance
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
            Instance = new OpenSettings(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is MenuCommandService mcs)
            {
                var cmdGroup = typeof(VSConstants.VSStd97CmdID).GUID;
                var cmd = new CommandID(cmdGroup, VSConstants.cmdidToolsOptions);
                mcs.GlobalInvoke(cmd, typeof(FormatterOptions).GUID.ToString());
            }
        }
    }
}
