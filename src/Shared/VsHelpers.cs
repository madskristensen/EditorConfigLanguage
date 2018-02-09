using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace EditorConfig
{
    public static class VsHelpers
    {
        private static IVsUIShell5 _shell = GetService<SVsUIShell, IVsUIShell5>();
        private static IVsSolution5 _solution = GetService<IVsSolution, IVsSolution5>();
        private static IComponentModel _compositionService;

        internal static DTE2 DTE { get; } = GetService<DTE, DTE2>();

        public static TReturnType GetService<TServiceType, TReturnType>()
        {
            return (TReturnType)ServiceProvider.GlobalProvider.GetService(typeof(TServiceType));
        }

        /// <summary>
        /// Opens a file in the Preview Tab (provisional tab) if supported by the editor factory.
        /// </summary>
        public static void PreviewDocument(string file)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE2.NDS_TryProvisional, VSConstants.NewDocumentStateReason.Navigation))
            {
                var provider = new ServiceProvider(DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                VsShellUtilities.OpenDocument(provider, file);
            }
        }

        /// <summary>Gets the root folder of any Visual Studio project.</summary>
        public static string GetRootFolder(this Project project)
        {
            if (project == null)
                return null;

            if (project.IsKind("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")) // solution folder
                return Path.GetDirectoryName(DTE.Solution.FullName);

            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }

        public static ProjectItem AddFileToProject(this Project project, string file, string itemType = null)
        {
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT))
                return DTE.Solution.FindProjectItem(file);

            string root = project.GetRootFolder();

            if (string.IsNullOrEmpty(root) || !file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;

            ProjectItem item = project.ProjectItems.AddFromFile(file);
            item.SetItemType(itemType);
            return item;
        }

        public static void SetItemType(this ProjectItem item, string itemType)
        {
            try
            {
                if (item == null || item.ContainingProject == null)
                    return;

                if (string.IsNullOrEmpty(itemType) || item.ContainingProject.IsKind(ProjectTypes.WEBSITE_PROJECT, ProjectTypes.UNIVERSAL_APP))
                    return;

                item.Properties.Item("ItemType").Value = itemType;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (string guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string GetSelectedItemPath(out object selectedItem)
        {
            var items = (Array)DTE.ToolWindows.SolutionExplorer.SelectedItems;
            selectedItem = null;

            foreach (UIHierarchyItem selItem in items)
            {
                selectedItem = selItem.Object;

                if (selItem.Object is ProjectItem item && item.Properties != null)
                {
                    return item.Properties.Item("FullPath").Value.ToString();
                }
                else if (selItem.Object is Project proj && proj.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // solution folder
                {
                    return proj.GetRootFolder();
                }
            }

            return Path.GetDirectoryName(DTE.Solution.FullName);
        }

        public static string GetFileName(this ITextBuffer buffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter))
                return null;

            string ppzsFilename = null;
            int returnCode = -1;

            if (bufferAdapter is IPersistFileFormat persistFileFormat)
                try
                {
                    returnCode = persistFileFormat.GetCurFile(out ppzsFilename, out uint pnFormatIndex);
                }
                catch (NotImplementedException)
                {
                    return null;
                }

            if (returnCode != VSConstants.S_OK)
                return null;

            return ppzsFilename;
        }

        public static IServiceProvider AsServiceProvider(this DTE2 dte)
        {
            return new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
        }

        public static void SatisfyImportsOnce(this object o)
        {
            if (_compositionService == null)
            {
                _compositionService = GetService<SComponentModel, IComponentModel>();
            }

            if (_compositionService != null)
            {
                _compositionService.DefaultCompositionService.SatisfyImportsOnce(o);
            }
        }

        public static void OpenFile(string fileName)
        {
            VsShellUtilities.OpenDocument(DTE.AsServiceProvider(), fileName);
            DTE.ExecuteCommandSafe("SolutionExplorer.SyncWithActiveDocument");
            DTE.ActiveDocument.Activate();
        }

        public static void ExecuteCommandSafe(this DTE2 dte, string commandName)
        {
            Command command = dte.Commands.Item(commandName);
            if (command.IsAvailable)
            {
                dte.Commands.Raise(command.Guid, command.ID, null, null);
            }
        }

        public static BitmapSource ToBitmap(this ImageMoniker moniker, int size)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            uint backgroundColor = VsColors.GetThemedColorRgba(_shell, EnvironmentColors.BrandedUIBackgroundBrushKey);

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags | unchecked((uint)_ImageAttributesFlags.IAF_Background),
                //Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                Format = (uint)_UIDataFormat.DF_WPF,
                Dpi = 96,
                LogicalHeight = size,
                LogicalWidth = size,
                Background = backgroundColor,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            var service = (IVsImageService2)Package.GetGlobalService(typeof(SVsImageService));
            IVsUIObject result = service.GetImage(moniker, imageAttributes);
            result.get_Data(out object data);

            return data as BitmapSource;
        }
    }

    public static class ProjectTypes
    {
        public const string ASPNET_5 = "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}";
        public const string WEBSITE_PROJECT = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        public const string UNIVERSAL_APP = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        public const string NODE_JS = "{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";
        public const string SSDT = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
    }
}
