using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public class ErrorCatalog
    {
        public static List<Error> All = new List<Error>();

        public static Error DuplicateProperty = Create("EC101", ErrorCategory.Warning, Resources.Text.ValidationDuplicateProperty);
        public static Error DuplicateSection = Create("EC102", ErrorCategory.Warning, Resources.Text.ValidationDuplicateSection);
        public static Error GlobbingNoMatch = Create("EC103", ErrorCategory.Suggestion, Resources.Text.ValidationNoMatch);
        public static Error MissingSeverity = Create("EC104", ErrorCategory.Error, Resources.Text.ValidationMissingSeverity);
        public static Error MissingValue = Create("EC105", ErrorCategory.Error, Resources.Text.ValidationMissingPropertyValue);
        public static Error OnlyRootAllowd = Create("EC106", ErrorCategory.Error, Resources.Text.ValidateOnlyRootAllowed);
        public static Error ParentDuplicateProperty = Create("EC107", ErrorCategory.Suggestion, Resources.Text.ValidationParentPropertyDuplicate);
        public static Error RootInSection = Create("EC108", ErrorCategory.Warning, Resources.Text.ValidationRootInSection);
        public static Error SectionSyntaxError = Create("EC109", ErrorCategory.Error, Resources.Text.ValidationSectionSyntaxError);
        public static Error SeverityNotApplicable = Create("EC110", ErrorCategory.Error, Resources.Text.ValidationSeverityNotApplicable);
        public static Error UnknownElement = Create("EC111", ErrorCategory.Error, Resources.Text.ValidationUnknownElement);
        public static Error UnknownKeyword = Create("EC112", ErrorCategory.Warning, Resources.Text.ValidateUnknownKeyword);
        public static Error UnknownSeverity = Create("EC113", ErrorCategory.Warning, Resources.Text.ValidationInvalidSeverity);
        public static Error UnknownValue = Create("EC114", ErrorCategory.Warning, Resources.Text.InvalidValue);
        public static Error TabWidthUnneeded = Create("EC115", ErrorCategory.Suggestion, Resources.Text.ValidationTabWidthUnneeded);
        public static Error IndentSizeUnneeded = Create("EC116", ErrorCategory.Suggestion, Resources.Text.ValidationIndentSizeUnneeded);
        public static Error SpaceInSection = Create("EC117", ErrorCategory.Suggestion, Resources.Text.ValidationSpaceInSection);

        public static bool TryGetErrorCode(string code, out Error errorCode)
        {
            errorCode = All.FirstOrDefault(c => c.Code.Equals(code));
            return errorCode != null;
        }

        private static Error Create(string errorCode, ErrorCategory type, string message)
        {
            var ec = new Error(errorCode, type, message);
            All.Add(ec);
            return ec;
        }
    }
}
