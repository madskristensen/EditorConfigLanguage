using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using System.Collections.Generic;

namespace EditorConfig
{
    class TableEntriesSnapshot : TableEntriesSnapshotBase
    {
        private string _projectName;
        private DTE2 _dte;

        internal TableEntriesSnapshot(IEnumerable<ParseItem> result, string projectName, string fileName)
        {
            _dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            _projectName = projectName;

            foreach (ParseItem item in result)
            {
                Errors.AddRange(item.Errors);
            }

            Url = fileName;
        }

        public List<Error> Errors { get; } = new List<Error>();

        public override int VersionNumber { get; } = 1;

        public override int Count
        {
            get { return Errors.Count; }
        }

        public string Url { get; set; }

        public override bool TryGetValue(int index, string columnName, out object content)
        {
            content = null;

            if ((index >= 0) && (index < Errors.Count))
            {
                if (columnName == StandardTableKeyNames.DocumentName)
                {
                    content = Url;
                }
                else if (columnName == StandardTableKeyNames.ErrorCategory)
                {
                    content = Vsix.Name;
                }
                else if (columnName == StandardTableKeyNames.ErrorSource)
                {
                    content = Vsix.Name;
                }
                else if (columnName == StandardTableKeyNames.Line)
                {
                    content = Errors[index].Line;
                }
                else if (columnName == StandardTableKeyNames.Column)
                {
                    content = Errors[index].Column;
                }
                else if (columnName == StandardTableKeyNames.FullText || columnName == StandardTableKeyNames.Text)
                {
                    content = Errors[index].Description;
                }
                else if (columnName == StandardTableKeyNames.ErrorSeverity)
                {
                    content = GetSeverity(Errors[index]);
                }
                else if (columnName == StandardTableKeyNames.Priority)
                {
                    content = vsTaskPriority.vsTaskPriorityMedium;
                }
                else if (columnName == StandardTableKeyNames.ErrorSource)
                {
                    content = ErrorSource.Other;
                }
                else if (columnName == StandardTableKeyNames.BuildTool)
                {
                    content = Vsix.Name;
                }
                else if (columnName == StandardTableKeyNames.ErrorCode)
                {
                    content = Errors[index].ErrorCode;
                }
                else if (columnName == StandardTableKeyNames.ProjectName)
                {
                    content = _projectName;
                }
                else if (columnName == StandardTableKeyNames.HelpLink)
                {
                    content = Errors[index].HelpLink;
                }
            }

            return content != null;
        }

        private __VSERRORCATEGORY GetSeverity(Error error)
        {
            switch (error.ErrorType)
            {
                case ErrorType.Error:
                    return __VSERRORCATEGORY.EC_ERROR;
                case ErrorType.Warning:
                    return __VSERRORCATEGORY.EC_WARNING;
            }

            return __VSERRORCATEGORY.EC_MESSAGE;
        }
    }
}
