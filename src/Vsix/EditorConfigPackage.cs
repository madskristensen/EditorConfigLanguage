using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using task = System.Threading.Tasks.Task;

namespace EditorConfig
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]

    [ProvideLanguageService(typeof(EditorConfigLanguage), ContentTypes.EditorConfig, 101, EnableAdvancedMembersOption = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, EnableLineNumbers = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true)]
    [ProvideLanguageExtension(typeof(EditorConfigLanguage), ".editorconfig")]
    //[ProvideService(typeof(EditorConfigLanguage), IsAsyncQueryable = true)]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), ".editorconfig", 1000)]
    [ProvideAutoBraceCompletion(ContentTypes.EditorConfig)]

    //[ProvideAutoLoad(PackageGuidString)]
    //[ProvideUIContextRule(PackageGuidString,
    //    name: Vsix.Name,
    //    expression: "EditorConfig",
    //    termNames: new[] { "EditorConfig" },
    //    termValues: new[] { "HierSingleSelectionName:.editorconfig$" }
    //)]
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
        }

        //protected override task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        //{
        //    AddService(typeof(EditorConfigLanguage), CreateServiceAsync, true);

        //    var editorFactory = new EditorFactory(this, typeof(EditorConfigLanguage).GUID);
        //    RegisterEditorFactory(editorFactory);

        //    return base.InitializeAsync(cancellationToken, progress);
        //}

        //// Borrowed from https://github.com/github/VisualStudio/blob/master/src/GitHub.VisualStudio/GitHubPackage.cs
        //private async Task<object> CreateServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        //{
        //    if (serviceType == null)
        //        return null;

        //    string contract = AttributedModelServices.GetContractName(serviceType);
        //    var cm = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;

        //    if (cm == null)
        //        return null;

        //    return await task.Run(() => cm.DefaultExportProvider.GetExportedValueOrDefault<object>(contract));
        //}
    }
}
