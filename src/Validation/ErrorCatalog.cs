using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class ErrorCatalog
    {
        public static List<ErrorCode> All = new List<ErrorCode>();

        public static ErrorCode DuplicateProperty = Create("EC101", ErrorCategory.Warning, Resources.Text.ValidationDuplicateProperty);
        public static ErrorCode DuplicateSection = Create("EC102", ErrorCategory.Warning, Resources.Text.ValidationDuplicateSection);
        public static ErrorCode GlobbingNoMatch = Create("EC103", ErrorCategory.Suggestion, Resources.Text.ValidationNoMatch);
        public static ErrorCode MissingSeverity = Create("EC104", ErrorCategory.Error, Resources.Text.ValidationMissingSeverity);
        public static ErrorCode MissingValue = Create("EC105", ErrorCategory.Error, Resources.Text.ValidationMissingPropertyValue);
        public static ErrorCode OnlyRootAllowd = Create("EC106", ErrorCategory.Error, Resources.Text.ValidateOnlyRootAllowed);
        public static ErrorCode ParentDuplicateProperty = Create("EC107", ErrorCategory.Suggestion, Resources.Text.ValidationParentPropertyDuplicate);
        public static ErrorCode RootInSection = Create("EC108", ErrorCategory.Warning, Resources.Text.ValidationRootInSection);
        public static ErrorCode SectionSyntaxError = Create("EC109", ErrorCategory.Error, Resources.Text.ValidationSectionSyntaxError);
        public static ErrorCode SeverityNotApplicable = Create("EC110", ErrorCategory.Error, Resources.Text.ValidationSeverityNotApplicable);
        public static ErrorCode UnknownElement = Create("EC111", ErrorCategory.Error, Resources.Text.ValidationUnknownElement);
        public static ErrorCode UnknownKeyword = Create("EC112", ErrorCategory.Warning, Resources.Text.ValidateUnknownKeyword);
        public static ErrorCode UnknownSeverity = Create("EC113", ErrorCategory.Warning, Resources.Text.ValidationInvalidSeverity);
        public static ErrorCode UnknownValue = Create("EC114", ErrorCategory.Warning, Resources.Text.InvalidValue);
        public static ErrorCode TabWidthUnneeded = Create("EC115", ErrorCategory.Suggestion, Resources.Text.ValidationTabWidthUnneeded);
        public static ErrorCode IndentSizeUnneeded = Create("EC116", ErrorCategory.Suggestion, Resources.Text.ValidationIndentSizeUnneeded);
        public static ErrorCode SpaceInSection = Create("EC117", ErrorCategory.Suggestion, Resources.Text.ValidationSpaceInSection);

        public static bool TryGetErrorCode(string code, out ErrorCode errorCode)
        {
            errorCode = All.FirstOrDefault(c => c.Code.Equals(code));
            return errorCode != null;
        }

        private static ErrorCode Create(string errorCode, ErrorCategory type, string message)
        {
            var ec = new ErrorCode(errorCode, type, message);
            All.Add(ec);
            return ec;
        }
    }
}
