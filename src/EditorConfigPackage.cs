using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace EditorConfig
{
    [Guid(PackageGuids.guidEditorConfigPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]

    [ProvideLanguageService(typeof(EditorConfigLanguage), Constants.LanguageName, 101, EnableAdvancedMembersOption = true, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, EnableLineNumbers = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(EditorConfigLanguage), Constants.FileName)]
    [ProvideLanguageEditorOptionPage(typeof(Options), Constants.LanguageName, null, "Formatting", "#101", new[] { "editorconfig", "Editor Config" })]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), Constants.FileName, 1000)]
    [ProvideBraceCompletion(Constants.LanguageName)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class EditorConfigPackage : AsyncPackage
    {
        public static EditorConfigLanguage Language
        {
            get;
            private set;
        }

        public static Options Options
        {
            get;
            private set;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Language = new EditorConfigLanguage(this);
            Options = (Options)GetDialogPage(typeof(Options));

            var serviceContainer = this as IServiceContainer;
            serviceContainer.AddService(typeof(EditorConfigLanguage), Language, true);

            var editorFactory = new EditorFactory(this, typeof(EditorConfigLanguage).GUID);
            RegisterEditorFactory(editorFactory);

            await CreateEditorConfigFile.InitializeAsync(this);
        }
    }
}
