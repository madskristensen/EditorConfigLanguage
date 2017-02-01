using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;

namespace EditorConfig.Commands
{
    [Export(typeof(INodeExtender))]
    public class CreateEditorConfigFileAnyCodeProvider : INodeExtender
    {
        private IWorkspaceCommandHandler _handler = new CreateEditorConfigFileAnyCode();
        public IChildrenSource ProvideChildren(WorkspaceVisualNodeBase parentNode) => null;

        public IWorkspaceCommandHandler ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
        {
            if (parentNode is IFolderNode)
            {
                return _handler;
            }

            return null;
        }
    }

    public class CreateEditorConfigFileAnyCode : IWorkspaceCommandHandler
    {
        public bool IgnoreOnMultiselect => true;

        public int Priority => 100;

        public int Exec(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (IsSupportedCommand(pguidCmdGroup, nCmdID))
            {
                if (selection.Count == 1 && selection[0] is IFolderNode folder)
                {
                    string fileName = Path.Combine(folder.FullPath, Constants.FileName);

                    if (File.Exists(fileName))
                    {
                        MessageBox.Show(Resources.Text.EditorConfigFileAlreadyExist, Vsix.Name, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        File.WriteAllText(fileName, Constants.DefaultFileContent);
                        VsHelpers.OpenFile(fileName);
                    }

                    return VSConstants.S_OK;
                }
            }

            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        public bool QueryStatus(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, ref uint cmdf, ref string customTitle)
        {
            if (selection.Count != 1 || selection[0] is IFileNode)
            {
                return false;
            }

            if (IsSupportedCommand(pguidCmdGroup, nCmdID))
            {
                cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                return true;
            }

            return selection[0].Parent.QueryStatus(pguidCmdGroup, nCmdID, ref cmdf, ref customTitle);
        }

        private static bool IsSupportedCommand(Guid pguidCmdGroup, uint nCmdID)
        {
            return pguidCmdGroup == PackageGuids.guidEditorConfigPackageCmdSet && nCmdID == PackageIds.CreateEditorConfigFileAnyCodeId;
        }
    }
}
