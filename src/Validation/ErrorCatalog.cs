using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorConfig
{
    public static class ErrorCatalog
    {
        private static ValidationOptions _o = EditorConfigPackage.ValidationOptions;
        private static List<Error> _errors = new List<Error>();

        public static IReadOnlyList<Error> All
        {
            get
            {
                return _errors;
            }
        }

        public static Error DuplicateProperty =>
            Create("EC101", ErrorCategory.Warning, Resources.Text.ValidationDuplicateProperty, () => _o.EnableDuplicateProperties);
        public static Error DuplicateSection =>
            Create("EC102", ErrorCategory.Warning, Resources.Text.ValidationDuplicateSection, () => _o.EnableDuplicateSections);
        public static Error GlobbingNoMatch =>
            Create("EC103", ErrorCategory.Suggestion, Resources.Text.ValidationNoMatch, () => _o.EnableGlobbingMatcher);
        public static Error MissingSeverity =>
            Create("EC104", ErrorCategory.Error, Resources.Text.ValidationMissingSeverity);
        public static Error MissingValue =>
            Create("EC105", ErrorCategory.Error, Resources.Text.ValidationMissingPropertyValue);
        public static Error OnlyRootAllowd =>
            Create("EC106", ErrorCategory.Error, Resources.Text.ValidateOnlyRootAllowed);
        public static Error ParentDuplicateProperty =>
            Create("EC107", ErrorCategory.Suggestion, Resources.Text.ValidationParentPropertyDuplicate, () => _o.EnableDuplicateFoundInParent);
        public static Error RootInSection =>
            Create("EC108", ErrorCategory.Warning, Resources.Text.ValidationRootInSection);
        public static Error SectionSyntaxError =>
            Create("EC109", ErrorCategory.Error, Resources.Text.ValidationSectionSyntaxError);
        public static Error SeverityNotApplicable =>
            Create("EC110", ErrorCategory.Error, Resources.Text.ValidationSeverityNotApplicable);
        public static Error UnknownElement =>
            Create("EC111", ErrorCategory.Error, Resources.Text.ValidationUnknownElement);
        public static Error UnknownKeyword =>
            Create("EC112", ErrorCategory.Warning, Resources.Text.ValidateUnknownKeyword, () => _o.EnableUnknownProperties);
        public static Error UnknownSeverity =>
            Create("EC113", ErrorCategory.Warning, Resources.Text.ValidationInvalidSeverity);
        public static Error UnknownValue =>
            Create("EC114", ErrorCategory.Warning, Resources.Text.InvalidValue, () => _o.EnableUnknownValues);
        public static Error TabWidthUnneeded =>
            Create("EC115", ErrorCategory.Suggestion, Resources.Text.ValidationTabWidthUnneeded);
        public static Error IndentSizeUnneeded =>
            Create("EC116", ErrorCategory.Suggestion, Resources.Text.ValidationIndentSizeUnneeded);
        public static Error SpaceInSection =>
            Create("EC117", ErrorCategory.Suggestion, Resources.Text.ValidationSpaceInSection, () => !_o.AllowSpacesInSections);

        public static bool TryGetErrorCode(string code, out Error errorCode)
        {
            errorCode = All.FirstOrDefault(c => c.Code.Equals(code));
            return errorCode != null;
        }

        private static Error Create(string errorCode, ErrorCategory type, string message)
        {
            return Create(errorCode, type, message, () => true);
        }

        private static Error Create(string errorCode, ErrorCategory type, string message, Func<bool> isSupported)
        {
            var ec = new Error(errorCode, type, message, isSupported);
            _errors.Add(ec);

            return ec;
        }
    }
}
