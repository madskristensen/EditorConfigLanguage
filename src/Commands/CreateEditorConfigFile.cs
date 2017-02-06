using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;

namespace EditorConfig
{
    internal sealed class CreateEditorConfigFile
    {
        private readonly Package _package;

        private CreateEditorConfigFile(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var cmdId = new CommandID(PackageGuids.guidEditorConfigPackageCmdSet, PackageIds.CreateEditorConfigFileId);
            var menuItem = new MenuCommand(CreateFile, cmdId);
            commandService.AddCommand(menuItem);
        }
        public static CreateEditorConfigFile Instance
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
            Instance = new CreateEditorConfigFile(package, commandService);
        }

        private void CreateFile(object sender, EventArgs e)
        {
            var dte = VsHelpers.GetService<DTE, DTE2>();
            string folder = VsHelpers.GetSelectedItemPath(out object item);

            if (string.IsNullOrEmpty(folder))
                return;

            string fileName = Path.Combine(folder, Constants.FileName);

            if (File.Exists(fileName))
            {
                MessageBox.Show(Resources.Text.EditorConfigFileAlreadyExist, Vsix.Name, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                File.WriteAllText(fileName, Constants.DefaultFileContent);
                ProjectItem newItem = AddFileToHierarchy(item, fileName);

                if (newItem != null)
                {
                    VsHelpers.OpenFile(fileName);
                }
            }
        }

        private static ProjectItem AddFileToHierarchy(object item, string fileName)
        {
            if (item is Project proj)
            {
                Telemetry.TrackUserTask("FileAddedToProject");
                return proj.AddFileToProject(fileName, "None");
            }
            else if (item is ProjectItem projItem && projItem.ContainingProject != null)
            {
                Telemetry.TrackUserTask("FileAddedToFolder");
                return projItem.ContainingProject.AddFileToProject(fileName, "None");
            }
            else if (item is Solution2 solution)
            {
                Telemetry.TrackUserTask("FileAddedToSolution");
                return AddFileToSolution(fileName, solution);
            }

            return null;
        }

        private static ProjectItem AddFileToSolution(string fileName, Solution2 solution)
        {
            Project currentProject = null;

            foreach (Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.Name == "Solution Items")
                {
                    currentProject = project;
                    break;
                }
            }

            if (currentProject == null)
                currentProject = solution.AddSolutionFolder("Solution Items");

            return currentProject.AddFileToProject(fileName, "None");
        }

        private static string FindFolder(object item)
        {
            if (item == null)
                return null;

            string folder = null;

            if (item is ProjectItem projectItem)
            {
                string fileName = projectItem.FileNames[1];

                if (File.Exists(fileName))
                {
                    folder = Path.GetDirectoryName(fileName);
                }
                else
                {
                    folder = fileName;
                }
            }
            else if (item is Project project)
            {
                folder = project.GetRootFolder();
            }
            else if (item is Solution solution)
            {
                folder = Path.GetDirectoryName(solution.FileName);
            }

            return folder;
        }
    }
}
