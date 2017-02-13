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

    [ProvideLanguageService(typeof(EditorConfigLanguage), Constants.LanguageName, 101, ShowCompletion = true, EnableAsyncCompletion = true, EnableAdvancedMembersOption = true, HideAdvancedMembersByDefault = false, QuickInfo = true, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, EnableLineNumbers = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(EditorConfigLanguage), Constants.FileName)]
    [ProvideLanguageEditorOptionPage(typeof(FormatterOptions), Constants.LanguageName, null, "Formatting", "#101", new[] { "editorconfig", "formatting" })]
    [ProvideLanguageEditorOptionPage(typeof(ValidationOptions), Constants.LanguageName, null, "Validation", "#102", new[] { "editorconfig", "validation" })]
    [ProvideLanguageEditorOptionPage(typeof(CompletionOptions), Constants.LanguageName, null, "Intellisense", "#103", new[] { "editorconfig", "intellisenes", "completion" })]
    [ProvideLanguageCodeExpansion(typeof(EditorConfigLanguage), Constants.LanguageName, 0, Constants.LanguageName, null, SearchPaths = @"$PackageFolder$\CodeExpansions\Snippets")]
    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
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

        public static FormatterOptions FormatterOptions
        {
            get;
            private set;
        }

        public static ValidationOptions ValidationOptions
        {
            get;
            private set;
        }

        public static CompletionOptions CompletionOptions
        {
            get;
            private set;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Language = new EditorConfigLanguage(this);
            FormatterOptions = (FormatterOptions)GetDialogPage(typeof(FormatterOptions));
            ValidationOptions = (ValidationOptions)GetDialogPage(typeof(ValidationOptions));
            CompletionOptions = (CompletionOptions)GetDialogPage(typeof(CompletionOptions));

            var serviceContainer = this as IServiceContainer;
            serviceContainer.AddService(typeof(EditorConfigLanguage), Language, true);

            var editorFactory = new EditorFactory(this, typeof(EditorConfigLanguage).GUID);
            RegisterEditorFactory(editorFactory);

            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                CreateEditorConfigFile.Initialize(this, commandService);
                OpenSettings.Initialize(this, commandService);
                SuppressError.Initialize(this, commandService);
            }
        }
    }
}
