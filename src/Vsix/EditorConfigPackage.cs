using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace EditorConfig
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]

    [ProvideLanguageService(typeof(EditorConfigLanguage), ContentTypes.EditorConfig, 101, EnableAdvancedMembersOption = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, EnableLineNumbers = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(EditorConfigLanguage), ".editorconfig")]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), ".editorconfig", 1000)]
    [ProvideAutoBraceCompletion(ContentTypes.EditorConfig)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class EditorConfigPackage : Package
    {
        public const string PackageGuidString = "6736c72d-7a27-49f0-9153-413ff47963ef";

        public static EditorConfigLanguage Language
        {
            get;
            private set;
        }

        protected override void Initialize()
        {
            Language = new EditorConfigLanguage(this);

            var serviceContainer = this as IServiceContainer;
            serviceContainer.AddService(typeof(EditorConfigLanguage), Language, true);

            var editorFactory = new EditorFactory(this, typeof(EditorConfigLanguage).GUID);
            RegisterEditorFactory(editorFactory);

            CreateEditorConfigFile.Initialize(this);
        }
    }
}
