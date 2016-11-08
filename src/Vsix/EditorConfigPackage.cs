using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using task = System.Threading.Tasks.Task;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using System.ComponentModel.Composition;

namespace EditorConfig
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]

    [ProvideLanguageService(typeof(EditorConfigLanguage), ContentTypes.EditorConfig, 100, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, EnableLineNumbers = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true)]
    //[ProvideLanguageEditorOptionPage(typeof(Options), MarkdownLanguage.LanguageName, null, "Advanced", "#101", new[] { "markdown", "md" })]
    [ProvideLanguageExtension(typeof(EditorConfigLanguage), ".editorconfig")]
    [ProvideService(typeof(EditorConfigLanguage), IsAsyncQueryable = true)]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), ".editorconfig", 1000)]
    public sealed class EditorConfigPackage : AsyncPackage
    {
        public const string PackageGuidString = "6736c72d-7a27-49f0-9153-413ff47963ef";

        protected override task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            AddService(typeof(EditorConfigLanguage), CreateServiceAsync, true);

            var editorFactory = new EditorFactory(this, typeof(EditorConfigLanguage).GUID);
            RegisterEditorFactory(editorFactory);
            
            return base.InitializeAsync(cancellationToken, progress);
        }

        private async Task<object> CreateServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (serviceType == null)
                return null;

            string contract = AttributedModelServices.GetContractName(serviceType);
            var cm = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;

            if (cm == null)
                return null;

            return await task.Run(() => cm.DefaultExportProvider.GetExportedValueOrDefault<object>(contract));

        }
    }
}
