using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace EditorConfig
{
    public static class VsHelpers
    {
        internal static DTE2 DTE = Package.GetGlobalService(typeof(DTE)) as DTE2;

        public static string GetRootFolder(this Project project)
        {
            if (project == null)
                return null;

            if (project.IsKind(ProjectKinds.vsProjectKindSolutionFolder))
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

            var root = project.GetRootFolder();

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
            foreach (var guid in kindGuids)
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
                else if (selItem.Object is Project proj && proj.Kind != ProjectKinds.vsProjectKindSolutionFolder)
                {
                    return proj.GetRootFolder();
                }
            }

            return Path.GetDirectoryName(DTE.Solution.FullName);
        }

        public static string GetFileName(this ITextBuffer buffer)
        {
            if (!buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter))
                return null;

            var persistFileFormat = bufferAdapter as IPersistFileFormat;
            string ppzsFilename = null;
            int returnCode = -1;

            if (persistFileFormat != null)
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

        private static IComponentModel _compositionService;

        public static void SatisfyImportsOnce(this object o)
        {
            if (_compositionService == null)
            {
                _compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
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
            var command = dte.Commands.Item(commandName);
            if (command.IsAvailable)
            {
                dte.Commands.Raise(command.Guid, command.ID, null, null);
            }
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
